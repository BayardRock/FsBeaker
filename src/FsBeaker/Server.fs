namespace FsBeaker

open System
open System.Collections.Generic
open System.Net

open FsBeaker.Kernel
open Newtonsoft.Json

open Suave
open Suave.Types
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.RequestErrors
open Suave.Http.Successful
open Suave.Http.Writers
open Suave.Web

module Server =

    [<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
    type EvaluateResponse = {
        [<JsonProperty("expression")>]
        Expression: string

        [<JsonProperty("status")>]
        Status: ExecuteReponseStatus

        [<JsonProperty("result")>]
        Result: BinaryOutput
    }

    [<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
    type EvaluateRequest = {
        ShellId: string
        Code: string
    }

    [<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
    type IntellisenseRequest = {
        ShellId: string
        Code: string
        LineIndex: int
        CharIndex: int
    }

    let internal shells = Dictionary<string, ConsoleKernelClient>()
    let internal findShell shellId = 
        let shell = shells.[shellId]
        if shell.Process.HasExited then
            failwithf "Kernel with shellId `%s` exited unexpectedly" shellId
        shell

    /// Creates a new shell with the specified id
    let internal newShell(shellId) = 
        if shells.ContainsKey(shellId) then
            shellId
        else
            let shellId = Guid.NewGuid().ToString()
            shells.Add(shellId, ConsoleKernelClient.StartNewProcess())
            shellId

    /// Starts the server on the specified port
    let start port =
    
        /// The configuration is the default listening to localhost:port (127.0.0.1).
        let config = {
            default_config with
                bindings = [ { scheme = HTTP; ip = IPAddress.Parse("127.0.0.1"); port = port } ]
            }

        /// Scrubs the code of tabs and replaces them with four spaces
        let scrubCode(code:string) = code.Replace("\t", "    ")

        /// Requires that a parameter be present by name in the request. If the parameter
        /// is not in the request, then BAD_REQUEST is returned, otherwise the function is called
        /// with the parsed out parameter.
        let required request parameterName f =
            let q = form request
            match q ^^ parameterName with
            | None -> BAD_REQUEST("Parameter not supplied " + parameterName)
            | Some(v) -> f(v)

        /// Requires that a parameter be present by name in the request and be an integer. If the parameter
        /// is not in the request, then BAD_REQUEST is returned. If the parameter is not an integer, then 
        /// BAD_REQUEST is returned. If everything checks out, then the function is called with the parsed
        /// out integer
        let requiredInt request parameterName f =
            required request parameterName (fun v ->
                match Int32.TryParse(v) with
                | false, _ -> BAD_REQUEST("Expected integer for parameter " + parameterName)
                | true, v -> f(v)
            )

        /// Serializes the specified object into a JSON string
        let jsonOK o = JsonConvert.SerializeObject(o) |> OK
    
        /// Always returns "ok"
        let ready _ = OK <| "ok"

        /// The getShell API call
        let getShell r = 
            required r "shellId" (fun shellId ->
                OK <| newShell(shellId)
            )

        /// The evaluate API call
        let evaluate r = 
            required r "shellId" (fun shellId ->
                required r "code" (fun code ->
                    let shell = findShell shellId
                    let res = shell.Execute code
                    {
                        Expression = code
                        Status = res.Status
                        Result = res.Result
                    }
                    |> jsonOK
                )
            )

        /// The intellisense API call
        let intellisense r =
            required r "shellId" (fun shellId ->
                required r "code" (fun code ->
                    requiredInt r "lineIndex" (fun lineIndex ->
                        requiredInt r "charIndex" (fun charIndex ->
                            try
                                let shell = findShell shellId
                                let newCode = scrubCode code
                                shell.Intellisense(newCode, lineIndex, charIndex) |> jsonOK
                            with
                                ex ->
                                    stderr.WriteLine(ex.Message)
                                    stderr.WriteLine(ex.StackTrace)
                                    { Declarations = [||]; StartIndex = 0 } |> jsonOK
                        )
                    )
                )
            )

        /// Exits the the specified shell
        let exit r =
            required r "shellId" (fun shellId ->
                if shells.ContainsKey(shellId) then 
                    shells.[shellId].Process.Kill()
                    shells.Remove(shellId) |> ignore
                OK <| "ok"
            )

        let app = 
            choose [
                POST >>= choose [
                    url "/fsharp/ready"             >>= set_header "Content-Type" "text/plain"       >>= request ready
                    url "/fsharp/getShell"          >>= set_header "Content-Type" "text/plain"       >>= request getShell
                    url "/fsharp/evaluate"          >>= set_header "Content-Type" "application/json" >>= request evaluate
                    url "/fsharp/intellisense"      >>= set_header "Content-Type" "application/json" >>= request intellisense
                    url "/fsharp/exit"              >>= set_header "Content-Type" "application/json" >>= request exit
                    url "/fsharp/cancelExecution"   >>= OK "Not yet implemented"
                    url "/fsharp/killAllThreads"    >>= OK "Not yet implemented"
                    url "/fsharp/resetEnvironment"  >>= OK "Not yet implemented"
                    url "/fsharp/setShellOptions"   >>= OK "Not yet implemented"
                ]
                NOT_FOUND "404"
            ]

        stdout.WriteLine("Successfully started server")
        web_server config app
