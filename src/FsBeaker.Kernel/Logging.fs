namespace FsBeaker.Kernel

/// Logging module. This is really crude right now and just writes
/// to log.txt using File.WriteAllText(...)
module Logging = 
    
    open System.IO
    open System
    
    let fileName = "log.txt"

    let internal log(msg) = 
        try File.AppendAllText("log.txt", msg) with _ -> ()
        stdout.WriteLine(msg)

    let logMessage(m) = log(m)

    let logError(m) = log(m)

    let logInfo(m) = log(m)

    let logException(e:Exception) =
        log(e.Message)
        log(e.CompleteStackTrace())
