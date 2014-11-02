namespace FsBeaker.Kernel

open System
open System.IO
open System.Text
open Newtonsoft.Json
open System.Diagnostics
open System.Threading
open System.Reflection

[<CLIMutable; JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type Table = {
    columnNames: string[]
    values: string[][]
}

[<CLIMutable; JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type AutoCompleteRequest = { code: string; caretPosition: int }

[<CLIMutable; JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type AutoCompleteResponse = { declarations: string [] }

[<CLIMutable; JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type ExecuteRequest = { code: string }

[<CLIMutable; JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type ExecuteResponse = { result: BinaryOutput; status: ExecuteReponseStatus }
and ExecuteReponseStatus = OK = 0 | Error = 1

[<JsonObject(MemberSerialization = MemberSerialization.OptOut)>]
type ShellRequest =
    | AutoComplete of AutoCompleteRequest
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

        if line = null then None else Some <| sb.ToString()
        
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
    let sendObj(o:obj) = 
        let ser = JsonSerializer()
        let writer = new StringWriter()
        ser.Serialize(writer, o)
        sendLine <| writer.ToString()
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

            { result = result; status = ExecuteReponseStatus.OK }
        
        else

            { result = { ContentType = "text/plain"; Data = sbErr.ToString() }; status = ExecuteReponseStatus.Error }

    /// Processes a request to execute some code
    let processExecute(req: ExecuteRequest) =

        // clear errors and any output
        sbOut.Clear() |> ignore
        sbErr.Clear() |> ignore

        // evaluate
        let response = 
            try
                eval req.code
            with _ -> 
                { result = { ContentType = "text/plain"; Data = sbErr.ToString().Trim() }; status = ExecuteReponseStatus.Error }

        sendObj response

    /// Processes a request to perform auto complete. Currently filtering is being performed
    /// here. This can be moved to the client once access to the CodeMirror object is obtained
    let processAutoComplete(req: AutoCompleteRequest) =
        let (decls, filterString) = GetDeclarations(req.code, req.caretPosition)
        let filteredDecls = 
            decls.Items
            |> Seq.filter (fun x -> x.Name.StartsWith(filterString, StringComparison.OrdinalIgnoreCase))
            |> Seq.map (fun x -> x.Name)
            |> Seq.toArray

        sendObj { declarations = filteredDecls }

    /// Process commands
    let processCommands block = 
        let shellRequest = JsonConvert.DeserializeObject<ShellRequest>(block)
        match shellRequest with
        | AutoComplete(x) -> processAutoComplete(x)
        | Execute(x) -> processExecute(x)

    /// The main loop
    let rec loop() =
        let block = readBlock stdin
        match block with
        | Some (json) -> 
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

    /// Sends on object to the process
    let sendObj (o:obj) =
        let v = 
            match o with 
            | :? AutoCompleteRequest as x -> AutoComplete(x)
            | :? ExecuteRequest as x -> Execute(x)
            | _ -> failwith "Invalid object to send"

        let json = JsonConvert.SerializeObject(v)
        writer.WriteLine(json)
        writer.WriteLine(separator)
        writer.Flush()

    /// Sends an object to the process and blocks until something is sent back
    let sendAndGet o =
        sendObj o
        readBlock reader

    /// The process
    member __.Process = p
    
    /// Executes the specified code and returns the results
    member __.Execute(req: ExecuteRequest) =
        match sendAndGet req with
        | Some (returnJson) -> JsonConvert.DeserializeObject<ExecuteResponse>(returnJson)
        | None -> failwith "Stream ended unexpectedly"

    /// Convenience method for executing a command
    member __.Execute(code) =
        __.Execute({ code = code })

    /// Performs auto complete functionality
    member __.Autocomplete(req: AutoCompleteRequest) =
        match sendAndGet req with
        | Some (returnJson) -> JsonConvert.DeserializeObject<AutoCompleteResponse>(returnJson)
        | None -> failwith "Stream ended unexpectedly"

    /// Performs auto complete functionality
    member __.Autocomplete(code, caretPosition) =
        __.Autocomplete({ code = code; caretPosition = caretPosition })

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