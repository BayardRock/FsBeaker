namespace FsBeaker.Kernel

open System
open System.IO
open System.Text
open Newtonsoft.Json
open System.Diagnostics
open System.Reflection

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type Table = {
    [<JsonProperty("columnNames")>]
    ColumnNames: string[]

    [<JsonProperty("values")>]
    Values: string[][]
}

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type IntellisenseRequest = {
    [<JsonProperty("code")>]
    Code: string
    
    [<JsonProperty("lineIndex")>]
    LineIndex: int

    [<JsonProperty("charIndex")>]
    CharIndex: int
}

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type IntellisenseResponse = {
    [<JsonProperty("declarations")>]
    Declarations: SimpleDeclaration[]

    [<JsonProperty("startIndex")>]
    StartIndex: int
}

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type ExecuteRequest = {
    [<JsonProperty("code")>]
    Code: string
}

[<CLIMutable(); JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type ExecuteResponse = {
    [<JsonProperty("result")>]
    Result: BinaryOutput

    [<JsonProperty("status")>]
    Status: ExecuteReponseStatus
}
and ExecuteReponseStatus = OK = 0 | Error = 1

[<JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type ShellRequest =
    | Intellisense of IntellisenseRequest
    | Execute of ExecuteRequest

[<AutoOpen>]
module KernelInternals = 

    let separator = "##"

    /// Alias for reader.ReadLine()
    let readLine(reader: TextReader) = 
        reader.ReadLine()

    /// Keeps reading from the reader until "##" is encountered
    let readBlock(reader: TextReader) = 
        let sb = StringBuilder()
        let mutable line = readLine reader
        while line <> separator && line <> null do
            sb.AppendLine(line) |> ignore
            line <- readLine reader

        if line = null then 
            None 
        else 
            let bytes = Convert.FromBase64String(sb.ToString())
            let json = Encoding.UTF8.GetString(bytes)
            Some(json, sb.ToString())

    /// Serializes an object to a string
    let serialize(o) =
        let ser = JsonSerializer()
        let writer = new StringWriter()
        ser.Serialize(writer, o)
        writer.ToString()
        
/// The console kernel handles console requests and responds by sending
/// json data back to the console
type ConsoleKernel() =
   
    /// Gets the header code to prepend to all items
    let headerCode = 
        let file = FileInfo(Assembly.GetEntryAssembly().Location)
        let dir = file.Directory.FullName
        let includeFile = Path.Combine(dir, "Include.fsx")
        let code = File.ReadAllText(includeFile)
        String.Format(code, dir.Replace("\\", "\\\\"))

    /// Sends a line
    let sendLine(str:string) = 
        stdout.WriteLine(str)

    /// Sends an object with the separator
    let sendObj(o) = 
        let json = JsonConvert.SerializeObject(o)
        let bytes = Encoding.UTF8.GetBytes(json)
        let encodedJson = Convert.ToBase64String(bytes)
        sendLine <| encodedJson
        sendLine <| separator

    /// Evaluates the specified code
    let eval (code: string) =

        fsiEval.EvalInteraction(code)

        let error = sbErr.ToString()
        if String.IsNullOrWhiteSpace(error) then 

            // return results (not yet)
            let result = 
                match GetLastExpression() with
                | Some(it) -> 
                        
                    let secondaryType = 
                        match it.ReflectionValue with
                        | null -> typeof<obj>
                        | _ -> it.ReflectionValue.GetType()

                    let printer = Printers.findDisplayPrinter(it.ReflectionType, secondaryType)
                    let (_, callback) = printer
                    callback(it.ReflectionValue)

                | None -> 
                        
                    { ContentType = "text/plain"; Data = "" }

            { Result = result; Status = ExecuteReponseStatus.OK }
        
        else

            { Result = { ContentType = "text/plain"; Data = sbErr.ToString() }; Status = ExecuteReponseStatus.Error }

    /// Processes a request to execute some code
    let processExecute(req: ExecuteRequest) =

        // clear errors and any output
        sbOut.Clear() |> ignore
        sbErr.Clear() |> ignore

        // evaluate
        let response = 
            try
                eval req.Code
            with ex -> 
                { Result = { ContentType = "text/plain"; Data = ex.Message + ": " + sbErr.ToString() }; Status = ExecuteReponseStatus.Error }

        sendObj response

    /// Gets the intellisense information and sends it back
    let processIntellisense(req: IntellisenseRequest) =
        let (decls, startIndex) = GetDeclarations(req.Code, req.LineIndex, req.CharIndex)
        sendObj { Declarations = decls; StartIndex = startIndex }

    /// Process commands
    let processCommands block = 
        let shellRequest = JsonConvert.DeserializeObject<ShellRequest>(block)
        match shellRequest with
        | Intellisense(x) -> processIntellisense(x)
        | Execute(x) -> processExecute(x)

    /// The main loop
    let rec loop() =
        let block = readBlock stdin
        match block with
        | Some (json, _) -> 
            processCommands json
            loop()
        | None ->
            failwith "Stream ended unexpectedly"

    /// Executes the header code and then carries on
    let start() = 
        ignore <| eval headerCode
        loop()

    // Start the kernel by looping forever
    member __.Start() = start()

/// API for sending commands to a ConsoleKernel
type ConsoleKernelClient(p: Process) = 

    let reader = p.StandardOutput
    let writer = p.StandardInput

    /// Sends a line
    let sendLine(str:string) = 
        writer.WriteLine(str)
        writer.Flush()

    /// Sends an object to the process and blocks until something is sent back
    let sendAndGet(o:obj) =

        /// Sends on object to the process
        let sendObj() =

            let v = 
                match o with 
                | :? IntellisenseRequest as x -> Intellisense(x)
                | :? ExecuteRequest as x -> Execute(x)
                | _ -> failwith "Invalid object to send"

            let json = JsonConvert.SerializeObject(v)
            let bytes = Encoding.UTF8.GetBytes(json)
            let encodedJson = Convert.ToBase64String(bytes)
            sendLine <| encodedJson
            sendLine <| separator

        lock p (fun () ->
            sendObj()
            readBlock reader)

    /// The process
    member __.Process = p
    
    /// Executes the specified code and returns the results
    member __.Execute(req: ExecuteRequest) =
        match sendAndGet req with
        | Some (returnJson, raw) -> JsonConvert.DeserializeObject<ExecuteResponse>(returnJson)
        | None -> failwith "Stream ended unexpectedly"

    /// Convenience method for executing a command
    member __.Execute(code) =
        __.Execute({ Code = code })

    /// Performs intellisense functionality
    member __.Intellisense(req: IntellisenseRequest) =
        match sendAndGet req with
        | Some (returnJson, raw) -> JsonConvert.DeserializeObject<IntellisenseResponse>(returnJson)
        | None -> failwith "Stream ended unexpectedly"

    /// Performs intellisense functionality
    member __.Intellisense(code, lineIndex, charIndex) =
        __.Intellisense({ Code = code; LineIndex = lineIndex; CharIndex = charIndex })

    /// IDisposable, disposes of the process    
    interface IDisposable with
        
        /// Dispose of the process
        member __.Dispose() = 
            p.Kill()
            p.Dispose()

    /// Show the dispose method
    member __.Dispose() = (__ :> IDisposable).Dispose()

    /// Starts a new instance of FsBeaker.Kernel.exe
    static member StartNewProcess() =
        let procStart = ProcessStartInfo("FsBeaker.Kernel.exe")
        procStart.RedirectStandardError <- true
        procStart.RedirectStandardInput <- true
        procStart.RedirectStandardOutput <- true
        procStart.UseShellExecute <- false
        procStart.CreateNoWindow <- true

        new ConsoleKernelClient(Process.Start(procStart))