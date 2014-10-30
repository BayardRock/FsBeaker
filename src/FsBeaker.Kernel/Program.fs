open FsBeaker.Kernel
open System.IO
open System.Threading

[<EntryPoint>]
let main _ = 

    Printers.addDefaultDisplayPrinters()

    try
        let kernel = ConsoleKernel()
        kernel.Start()
        0
    with 
        ex -> 
            stdout.WriteLine(ex.Message)
            stdout.WriteLine(ex.CompleteStackTrace())
            File.AppendAllText("log.txt", ex.Message)
            File.AppendAllText("log.txt", ex.CompleteStackTrace())
            -1