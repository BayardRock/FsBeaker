open FsBeaker.Kernel
open System.IO

[<EntryPoint>]
let main _ = 

    Printers.addDefaultDisplayPrinters()

    try
        let kernel = ConsoleKernel()
        kernel.Start()
        0
    with 
        ex -> 
            let log(msg) = 
                try File.AppendAllText("log.txt", msg) with _ -> ()
                stdout.WriteLine(msg)

            let err = Evaluation.sbErr.ToString()
            let std = Evaluation.sbOut.ToString()

            log ("Evaluation ERR: " + err)
            log ("Evaluation STD: " + std)

            log ("Stack trace:")
            log (ex.Message)
            log (ex.CompleteStackTrace())
            log ("")
            
            -1