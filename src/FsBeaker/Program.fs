open System
open System.Diagnostics
open System.IO
open FsBeaker
open Nowin
open Microsoft.Owin.Hosting
open Microsoft.Owin.Builder
open Newtonsoft.Json

[<EntryPoint>]
let main args = 

    // get the port from the args
    let port = 
        match args with
        | [| port |] ->
            match Int32.TryParse(port) with
            | true, p -> Some p
            | false, _ -> None
        | _ -> Some 9000


    match port with
    | Some (p) ->

        try

            let builder = AppBuilder()
            Startup().Configuration(builder)
            let owinApp = builder.Build()
            let server = ServerBuilder.New().SetPort(p).SetOwinApp(owinApp).Build()
            server.Start()

            stdout.WriteLine("Successfully started server")
            let _ = stdin.ReadLine()
            0
        with ex ->
            stdout.WriteLine(ex.Message)
            stdout.WriteLine(ex.StackTrace)
            -1

    | None ->
        
        for p in Process.GetProcessesByName("FsBeaker.Kernel") do p.Kill()
        for p in Process.GetProcessesByName("nginx") do p.Kill()
        let launcher = args.[0]
        let psi = ProcessStartInfo(launcher)
        psi.WorkingDirectory <- Path.GetDirectoryName(launcher)
        Process.Start(psi) |> ignore
        0