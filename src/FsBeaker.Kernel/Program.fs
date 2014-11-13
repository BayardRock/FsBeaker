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
            let err = Evaluation.sbErr.ToString()
            let std = Evaluation.sbOut.ToString()

            stdout.WriteLine("Evaluation ERR: " + err)
            stdout.WriteLine("Evaluation STD: " + std)

            stdout.WriteLine("Stack trace:")
            stdout.WriteLine(ex.Message)
            stdout.WriteLine(ex.CompleteStackTrace())
            
            File.AppendAllText("log.txt", ex.Message)
            File.AppendAllText("log.txt", ex.CompleteStackTrace())
            -1