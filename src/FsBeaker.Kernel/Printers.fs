namespace FsBeaker.Kernel

open System
open System.Text
open System.Web
open FSharp.Charting
open FsBeaker.Charts

module Printers = 

    let mutable internal displayPrinters : list<Type * (obj -> BinaryOutput)> = []

    /// Convenience method for encoding a string within HTML
    let internal htmlEncode(str) = HttpUtility.HtmlEncode(str)

    /// Adds a custom display printer for extensibility
    let internal addDisplayPrinter(printer : 'T -> BinaryOutput) =
        displayPrinters <- (typeof<'T>, (fun (x:obj) -> printer (unbox x))) :: displayPrinters

    /// Default display printer
    let internal defaultDisplayPrinter(x) =
        { ContentType = "text/plain"; Data = sprintf "%A" x }

    /// Finds a display printer with the first type that is assignable from the specified type
    let internal findSingleDisplayPrinter(findType) =
        let printers = 
            displayPrinters
            |> Seq.filter (fun (t, _) -> t.IsAssignableFrom(findType))
            |> Seq.toList

        match printers with
        | [] -> None
        | _  -> Some printers.Head

    /// Finds a display printer based off of the type
    let internal findDisplayPrinter(findType, secondaryType) = 
        match findSingleDisplayPrinter(findType) with
        | Some(x) -> x
        | None ->
            match findSingleDisplayPrinter(secondaryType) with
            | Some(x) -> x
            | None -> (typeof<obj>, defaultDisplayPrinter)

    /// Adds default display printers
    let internal addDefaultDisplayPrinters() = 
        
        // add generic chart printer
        addDisplayPrinter(fun (x:ChartTypes.GenericChart) ->
            { ContentType = "image/png"; Data = x.ToPng() }
        )

        // add chart printer
        addDisplayPrinter(fun (x:GenericChartWithSize) ->
            { ContentType = "image/png"; Data = x.Chart.ToPng(x.Size) }
        )
        
        // add table printer
        addDisplayPrinter(fun (x:TableOutput) -> 
            { ContentType = "table/grid"; Data = x } 
        )

        // add html printer
        addDisplayPrinter(fun (x:HtmlOutput) ->
            { ContentType = "text/html"; Data = x.Html }
        )
        
        // add latex printer
        addDisplayPrinter(fun (x:LatexOutput) ->
            { ContentType = "text/latex"; Data = x.Latex }
        )

        // add binaryoutput printer
        addDisplayPrinter(fun (x:BinaryOutput) ->
            x
        )

        // add XYGraphics printer
        addDisplayPrinter(fun (x:FsBeaker.Charts.XYGraphics) ->
            { ContentType = "chart"; Data = x <|> Plot() }
        )

        // add XYChart printer
        addDisplayPrinter(fun (x:Plot) ->
            { ContentType = "chart"; Data = x }
        )

        // add CombinedPlot printer
        addDisplayPrinter(fun (x:FsBeaker.Charts.CombinedPlot) ->
            { ContentType = "chart"; Data = x }
        )

        // add JObject printer
        addDisplayPrinter(fun (x:Newtonsoft.Json.Linq.JObject) ->
            { ContentType = "text/plain"; Data = x.ToString(Newtonsoft.Json.Formatting.None) }
        )
