namespace FsBeaker.Controllers

open System
open System.Collections.Generic
open System.Web.Http
open System.Diagnostics
open FsBeaker.Kernel
open System.Net
open FsBeaker
open Newtonsoft.Json

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type EvaluateResponse = {
    expression: string
    status: ExecuteReponseStatus
    result: BinaryOutput
}

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type EvaluateRequest = {
    ShellId: string
    Code: string
}

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type HandShakeResponse = {
    id: string
    minimumVersion: string
    supportedConnectionTypes: string[]
    successful: bool
    channel: string
    clientId: string
    version: string
}

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type HandShakeRequestItemAdvice = {
    timeout: int
    interval: int
}

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type HandshakeRequestItem = {
    id: string
    version: string
    minimumVersion: string
    supportedConnectionTypes: string[]
    advice: HandShakeRequestItemAdvice
}

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type AutocompleteRequest = {
    ShellId: string
    Code: string
    CaretPosition: int
}

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type AutocompleteResponse = {
    Declarations: string[]
}

[<AutoOpen>]
module FSharpControllerInternal = 

    let shells = Dictionary<string, ConsoleKernelClient>()
    let findShell shellId = 
        let shell = shells.[shellId]
        if shell.Process.HasExited then
            failwithf "Kernel with shellId `%s` exited unexpectedly" shellId
        shell

    /// Creates a new shell with the specified id
    let newShell() = 
        
        let shellId = Guid.NewGuid().ToString()
        shells.Add(shellId, ConsoleKernelClient.StartNewProcess())
        shellId

type FSharpController() =
    inherit ApiController()

    /// Scrubs the code of tabs and replaces them with four spaces
    let scrubCode(code:string) = code.Replace("\t", "    ")

    /// Convenience method for sending a raw string
    member __.Raw(s) =
        __.Content(HttpStatusCode.OK, s, StringMediaTypeFormatter())

    /// Stubbed out for later
    [<HttpPost>]
    member __.GetShell() = 
        __.Raw(newShell())

    /// Stubbed out for later
    [<HttpPost>]
    member __.Evaluate(req: EvaluateRequest) = 

        let code = scrubCode req.Code
        try
            let shell = findShell req.ShellId
            let res = shell.Execute(code)
            {
                expression = code
                status = res.status
                result = res.result
            }
        with 
            ex -> 
                stderr.WriteLine(ex.Message)
                stderr.WriteLine(ex.StackTrace)
                {
                    expression = code
                    status = ExecuteReponseStatus.Error
                    result = { ContentType = "text/plain"; Data = ex.Message } 
                }   

    /// Gets the auto completions
    [<HttpPost>]
    member __.Autocomplete(req: AutocompleteRequest) =
        try
            let shell = findShell req.ShellId
            let code = scrubCode req.Code
            let res = shell.Autocomplete(code, req.CaretPosition)
            { Declarations = res.declarations }
        with 
            ex -> 
                stderr.WriteLine(ex.Message)
                stderr.WriteLine(ex.StackTrace)
                { Declarations = [||] }

    /// Stubbed out for later
    [<HttpPost>]
    member __.Exit(shellId: string) =
        let shell = findShell shellId
        shell.Process.Kill()
        shells.Remove(shellId)
    
    /// Stubbed out for later
    [<HttpPost>]
    member __.CancelExecution(shellId: string) =
        "Not yet implemented"
    
    /// Stubbed out for later
    [<HttpPost>]
    member __.KillAllThreads(shellId: string) =
        "Not yet implemented"

    /// Stubbed out for later
    [<HttpPost>]
    member __.ResetEnvironment(shellId: string) =
        "Not yet implemented"

    /// Stubbed out for later
    [<HttpPost>]
    member __.SetShellOptions(shellId: string) =
        "Not yet implemented"

    /// Stubbed out for later
    [<HttpPost>]
    member __.SetShellOptions() =
        "Not yet implemented"
