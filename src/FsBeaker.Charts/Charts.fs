namespace FsBeaker.Charts

open System
open System.Drawing
open System.Linq
open Newtonsoft.Json
open Newtonsoft.Json.Converters

[<AutoOpen>]
module BeakerChartInternals = 
    
    let BlackColor = "#FF000000"
    let DefaultColor = "#FFC0504D"
    let DefaultOutlineColor = BlackColor
    let pallete = 
        [|
            String.Format("#{0:X}", Color.FromArgb(192, 80, 77).ToArgb())
            String.Format("#{0:X}", Color.FromArgb(79, 129, 189).ToArgb())
            String.Format("#{0:X}", Color.FromArgb(155, 187, 89).ToArgb())
            String.Format("#{0:X}", Color.FromArgb(248, 150, 70).ToArgb())
            String.Format("#{0:X}", Color.FromArgb(128, 100, 162).ToArgb())
            String.Format("#{0:X}", Color.FromArgb(75, 172, 198).ToArgb())
        |]

    let getColor i = pallete.[i % pallete.Length]

    /// This is useful when generating code, kept here
    let internal genCode(items:string) = 
        let variables = 
            items.Split(',')
            |> Seq.map (fun x -> x.Trim().TrimStart([|'?'|]))

        let str = String.Join(", ", [| for v in variables do yield "?" + v + " = " + v |])
        str



type ConstantLine() = class end
type ConstantBand() = class end
type Text() = class end
type TimeZone() = class end

[<JsonConverter(typeof<StringEnumConverter>)>]
type StrokeType = 
    | NONE = 0
    | SOLID = 1
    | DASH = 2
    | DOT = 3
    | DASHDOT = 4
    | LONGDASH = 5

[<JsonConverter(typeof<StringEnumConverter>)>]
type ShapeType = 
    | SQUARE = 0
    | CIRCLE = 1
    | TRIANGLE = 2
    | DIAMOND = 3
    | DCROSS = 4
    | DOWNTRIANGLE = 5
    | CROSS = 6
    | DEFAULT = 7
    | LEVEL = 8
    | VLEVEL = 9
    | LINECROSS = 10

type YAxis() =
    
    [<JsonProperty("type")>]
    member val Type = "YAxis"

    [<JsonProperty("label")>]
    member val Label = "" with get, set
    
    [<JsonProperty("auto_range")>]
    member val AutoRange = true with get, set
    
    [<JsonProperty("auto_range_includes_zero")>]
    member val AutoRangeIncludesZero = true with get, set
    
    [<JsonProperty("lower_margin")>]
    member val LowerMargin = 0.05 with get, set
    
    [<JsonProperty("upper_margin")>]
    member val UpperMargin = 0.05 with get, set
    
    [<JsonProperty("lower_bound")>]
    member val LowerBound = 0.0 with get, set
    
    [<JsonProperty("upper_bound")>]
    member val UpperBound = 0.0 with get, set
    
    [<JsonProperty("log")>]
    member val Log = false with get, set
    
    [<JsonProperty("log_base")>]
    member val LogBase : int Option = None with get, set

type Crosshair() =

    [<JsonProperty("style")>]
    member val Style = StrokeType.SOLID with get, set

    [<JsonProperty("width")>]
    member val Width = 1.0 with get, set

    [<JsonProperty("color")>]
    member val Color = DefaultColor with get, set

[<AbstractClass>]
type XYGraphics() =

    [<JsonProperty("type")>]
    abstract member Type : string with get

    [<JsonProperty("x")>]
    member val X = [|0.0|] with get, set

    [<JsonProperty("y")>]
    member val Y = [|0.0|] with get, set

    [<JsonProperty("visible")>]
    member val Visible = true with get, set

    [<JsonProperty("display_name")>]
    member val DisplayName = "" with get, set
    
    [<JsonProperty("y_axis_name")>]
    member val YAxisName = "" with get, set

    [<JsonProperty("color")>]
    member val Color : string = null with get, set

    /// Sets the data from a sequence of tuples
    member self.Data(data: seq<float * float>) =
        self.X <- data |> Seq.map fst |> Seq.toArray
        self.Y <- data |> Seq.map snd |> Seq.toArray
        self

    /// Sets the data from a sequence of tuples
    abstract member Data : seq<float> -> XYGraphics
    default self.Data(data: seq<float>) =
        self.X <- data |> Seq.toArray
        self.Y <- self.X |> Array.mapi (fun i x -> float i)
        self

    /// Convenience operator for setting the data
    static member (<|>) (c : 'T when 'T :> XYGraphics, d : seq<float>) = 
        c.Data(d) :?> 'T

    /// Convenience operator for setting the data
    static member (<|>) (c : 'T when 'T :> XYGraphics, d : seq<float * float>) =
        c.Data(d) :?> 'T

    /// Convenience operator for setting the data
    static member (<|>) (d : seq<float>, c : 'T when 'T :> XYGraphics) =
        c.Data(d) :?> 'T

    /// Convenience operator for setting the data
    static member (<|>) (d : seq<float * float>, c : 'T when 'T :> XYGraphics) =
        c.Data(d) :?> 'T

type Area() =
    inherit XYGraphics()

    override __.Type = "Area"

    [<JsonProperty("base")>]
    member val Base = 0.0 with get, set

    [<JsonProperty("bases")>]
    member val Bases : float[] Option = None with get, set

    [<JsonProperty("interpolation")>]
    member val Interpolation = 0 with get, set

type Bar() =
    inherit XYGraphics()

    override __.Type = "Bars"

    [<JsonProperty("width")>]
    member val Width = 0.0 with get, set

    [<JsonProperty("widths")>]
    member val Widths : float[] Option = None with get, set

    [<JsonProperty("base")>]
    member val Base = 0.0 with get, set

    [<JsonProperty("bases")>]
    member val Bases : float[] Option = None with get, set
    
    [<JsonProperty("colors")>]
    member val Colors : string[] Option = None with get, set

    [<JsonProperty("outline_color")>]
    member val OutlineColor = DefaultOutlineColor with get, set
    
    [<JsonProperty("outline_colors")>]
    member val OutlineColors : string[] Option = None with get, set

type Line() = 
    inherit XYGraphics()

    override __.Type = "Line"

    [<JsonProperty("width")>]
    member val Width = 1.5 with get, set

    [<JsonProperty("style")>]
    member val Style = StrokeType.SOLID with get, set

    [<JsonProperty("interpolation")>]
    member val Interpolation = 0 with get, set

type Point() = 
    inherit XYGraphics()

    override __.Type = "Points"

    [<JsonProperty("size")>]
    member val Size = 6.0 with get, set

    [<JsonProperty("sizes")>]
    member val Sizes : float[] Option = None with get, set

    [<JsonProperty("shape")>]
    member val Shape = ShapeType.DEFAULT with get, set

    /// TODO: don't serialize to null if None, just don't serialize the property
    [<JsonProperty("shapes")>]
    [<JsonIgnore()>]
    member val Shapes : ShapeType[] Option = None with get, set

    [<JsonProperty("fill")>]
    member val Fill = true with get, set

    [<JsonProperty("fills")>]
    member val Fills : bool[] Option = None with get, set

    [<JsonProperty("colors")>]
    member val Colors : string[] Option = None with get, set

    [<JsonProperty("outline_color")>]
    member val OutlineColor = DefaultOutlineColor with get, set

    [<JsonProperty("outline_colors")>]
    member val OutlineColors : string[] Option = None with get, set

    /// Sets the data from a sequence of tuples
    override self.Data(data: seq<float>) =
        self.Y <- data |> Seq.toArray
        self.X <- self.Y |> Array.mapi (fun i x -> float i)
        upcast self

type Stem() = 
    inherit XYGraphics()

    override __.Type = "Stems"

    [<JsonProperty("base")>]
    member val Base = 0.0 with get, set

    [<JsonProperty("bases")>]
    member val Bases : float[] Option = None with get, set

    [<JsonProperty("colors")>]
    member val Colors : string[] Option = None with get, set

    [<JsonProperty("style")>]
    member val Style = StrokeType.SOLID with get, set

    [<JsonProperty("styles")>]
    member val Styles : StrokeType[] Option = None with get, set

[<AbstractClass>]
type XYChart() = 

    [<JsonProperty("type")>]
    abstract member Type : string with get    

    [<JsonProperty("init_width")>]
    member val Width = 640 with get, set

    [<JsonProperty("init_height")>]
    member val Height = 480 with get, set

    [<JsonProperty("chart_title")>]
    member val Title = "" with get, set

    [<JsonProperty("domain_axis_label")>]
    member val XLabel = "" with get, set

    [<JsonProperty("y_label")>]
    member val YLabel = "" with get, set

    [<JsonProperty("show_legend")>]
    member val ShowLegend = false with get, set

    [<JsonProperty("use_tool_tip")>]
    member val UseToolTip = true with get, set

    [<JsonProperty("graphics_list")>]
    member val XYGraphics : XYGraphics list = [] with get, set

    [<JsonProperty("constant_lines")>]
    member val ConstantLines : ConstantLine[] = [||] with get, set

    [<JsonProperty("contant_bands")>]
    member val ConstantBands : ConstantBand[] = [||] with get, set

    [<JsonProperty("texts")>]
    member val Texts : Text[] = [||] with get, set

    [<JsonProperty("rangeAxes")>]
    member val YAxes = [| YAxis() |] with get, set

    [<JsonProperty("x_lower_margin")>]
    member val XLowerMargin = 0.05 with get, set

    [<JsonProperty("x_upper_margin")>]
    member val XUpperMargin = 0.05 with get, set

    [<JsonProperty("x_auto_range")>]
    member val XAutoRange = true with get, set

    [<JsonProperty("x_lower_bound")>]
    member val XLowerBound = 0.0 with get, set

    [<JsonProperty("x_upper_bound")>]
    member val XUpperBound = 0.0 with get, set

    [<JsonProperty("log_x")>]
    member val LogX = false with get, set

    [<JsonProperty("log_y")>]
    member val LogY = false with get, set

    [<JsonProperty("time_zone")>]
    member val TimeZone : TimeZone Option = None with get, set

    [<JsonProperty("crosshair")>]
    member val Crosshair : Crosshair Option = None with get, set

    /// Assign colors if they are not assigned
    member self.AssignColors() =
        let mutable i = 0
        for g in self.XYGraphics do
            if String.IsNullOrWhiteSpace(g.Color) then g.Color <- getColor i
            i <- i + 1

    /// Convenience operator for adding a graphics to the list of graphics objects
    static member (<|>) (c : #XYChart, g : XYGraphics) =
        c.XYGraphics <- c.XYGraphics @ [g]
        c.AssignColors()
        c

    /// Convenience operator for adding a graphics to the list of graphics objects
    static member (<|>) (g : XYGraphics, c : #XYChart) =
        c.XYGraphics <- c.XYGraphics @ [g]
        c.AssignColors()
        c

type Plot() =
    inherit XYChart()
        override __.Type = "Plot"

type TimePlot() =
    inherit XYChart()
        override __.Type = "Time"

type CombinedPlot() =

    [<JsonProperty("type")>]
    member val Type = "Combined" with get

    [<JsonProperty("init_width")>]
    member val InitWidth = 640 with get, set

    [<JsonProperty("init_height")>]
    member val InitHeight = 480 with get, set

    [<JsonProperty("title")>]
    member val Title = "" with get, set

    [<JsonProperty("x_label")>]
    member val XLabel = "" with get, set

    [<JsonProperty("subplots")>]
    member val Subplots : XYChart[] = [||] with get, set

    [<JsonProperty("weights")>]
    member val Weights : int[] = [||] with get, set

/// Static class convenient for generating charts and plots
/// data |> BeakerChartBeta.Line |> BeakerChartBeta.Plot
type BeakerChartBeta = 

    static member Area(?DisplayName, ?Color, ?Visible, ?Base, ?Bases, ?Interpolation) = 
        let c = Area()
        DisplayName     |> Option.iter c.set_DisplayName
        Color           |> Option.iter c.set_Color
        Visible         |> Option.iter c.set_Visible
        Base            |> Option.iter c.set_Base
        Bases           |> Option.iter c.set_Bases
        Interpolation   |> Option.iter c.set_Interpolation
        fun (data) -> c <|> data

    static member Area(data: seq<float * float>, ?DisplayName, ?Color, ?Visible, ?Base, ?Bases, ?Interpolation) = 
        data |> BeakerChartBeta.Area(?DisplayName = DisplayName, ?Color = Color, ?Visible = Visible, ?Base = Base, ?Bases = Bases, ?Interpolation = Interpolation)

    static member Bar(?DisplayName, ?Color, ?Visible, ?Width, ?Widths, ?Base, ?Bases, ?Colors, ?OutlineColor, ?OutlineColors) = 
        let c = Bar()
        DisplayName     |> Option.iter c.set_DisplayName
        Color           |> Option.iter c.set_Color
        Visible         |> Option.iter c.set_Visible
        Width           |> Option.iter c.set_Width
        Widths          |> Option.iter c.set_Widths
        Base            |> Option.iter c.set_Base
        Bases           |> Option.iter c.set_Bases
        Colors          |> Option.iter c.set_Colors
        OutlineColor    |> Option.iter c.set_OutlineColor
        OutlineColors   |> Option.iter c.set_OutlineColors
        fun (data) -> c <|> data

    static member Bar(data: seq<float * float>, ?DisplayName, ?Color, ?Visible, ?Width, ?Widths, ?Base, ?Bases, ?Colors, ?OutlineColor, ?OutlineColors) = 
        data |> BeakerChartBeta.Bar(?DisplayName = DisplayName, ?Color = Color, ?Visible = Visible, ?Width = Width, ?Widths = Widths, ?Base = Base, ?Bases = Bases, ?Colors = Colors, ?OutlineColor = OutlineColor, ?OutlineColors = OutlineColors)

    static member Line(?DisplayName, ?Color, ?Visible, ?Width, ?Style, ?Interpolation) = 
        let c = Line()
        DisplayName     |> Option.iter c.set_DisplayName
        Color           |> Option.iter c.set_Color
        Visible         |> Option.iter c.set_Visible
        Width           |> Option.iter c.set_Width
        Style           |> Option.iter c.set_Style
        Interpolation   |> Option.iter c.set_Interpolation
        fun (data) -> c <|> data

    static member Line(data: seq<float * float>, ?DisplayName, ?Color, ?Visible, ?Width, ?Style, ?Interpolation) = 
        data |> BeakerChartBeta.Line(?DisplayName = DisplayName, ?Color = Color, ?Visible = Visible, ?Width = Width, ?Style = Style, ?Interpolation = Interpolation)
    
    static member Point(?DisplayName, ?Color, ?Visible, ?Size, ?Sizes, ?Shape, ?Shapes, ?Fill, ?Fills, ?Colors, ?OutlineColor, ?OutlineColors) = 
        let c = Point()
        DisplayName     |> Option.iter c.set_DisplayName
        Color           |> Option.iter c.set_Color
        Visible         |> Option.iter c.set_Visible
        Size            |> Option.iter c.set_Size
        Sizes           |> Option.iter c.set_Sizes        
        Shape           |> Option.iter c.set_Shape        
        Shapes          |> Option.iter c.set_Shapes       
        Fill            |> Option.iter c.set_Fill         
        Fills           |> Option.iter c.set_Fills        
        Colors          |> Option.iter c.set_Colors       
        OutlineColor    |> Option.iter c.set_OutlineColor 
        OutlineColors   |> Option.iter c.set_OutlineColors
        fun (data) -> c <|> data

    static member Point(data: seq<float * float>, ?DisplayName, ?Color, ?Visible, ?Size, ?Sizes, ?Shape, ?Shapes, ?Fill, ?Fills, ?Colors, ?OutlineColor, ?OutlineColors) = 
        data |> BeakerChartBeta.Point(?DisplayName = DisplayName, ?Color = Color, ?Visible = Visible, ?Size = Size, ?Sizes = Sizes, ?Shape = Shape, ?Shapes = Shapes, ?Fill = Fill, ?Fills = Fills, ?Colors = Colors, ?OutlineColor = OutlineColor, ?OutlineColors = OutlineColors)

    static member Stem(?DisplayName, ?Color, ?Visible, ?Base, ?Bases, ?Colors, ?Style, ?Styles) = 
        let c = Stem()
        DisplayName     |> Option.iter c.set_DisplayName
        Color           |> Option.iter c.set_Color
        Visible         |> Option.iter c.set_Visible
        Base            |> Option.iter c.set_Base  
        Bases           |> Option.iter c.set_Bases 
        Colors          |> Option.iter c.set_Colors
        Style           |> Option.iter c.set_Style 
        Styles          |> Option.iter c.set_Styles
        fun (data) -> c <|> data

    static member Stem(data: seq<float * float>, ?DisplayName, ?Color, ?Visible, ?Base, ?Bases, ?Colors, ?Style, ?Styles) = 
        data |> BeakerChartBeta.Stem(?DisplayName = DisplayName, ?Color = Color, ?Visible = Visible, ?Base = Base, ?Bases = Bases, ?Colors = Colors, ?Style = Style, ?Styles = Styles)

    static member Plot(?Width, ?Height, ?Title, ?XLabel, ?YLabel, ?ShowLegend, ?UseToolTip, ?ConstantLines, ?ConstantBands, ?Texts, ?YAxes, ?XLowerMargin, ?XUpperMargin, ?XAutoRange, ?XLowerBound, ?XUpperBound, ?LogX, ?LogY, ?TimeZone, ?Crosshair) = 
        let p = Plot()
        Width           |> Option.iter p.set_Width
        Height          |> Option.iter p.set_Height
        Title           |> Option.iter p.set_Title
        XLabel          |> Option.iter p.set_XLabel
        YLabel          |> Option.iter p.set_YLabel
        ShowLegend      |> Option.iter p.set_ShowLegend
        UseToolTip      |> Option.iter p.set_UseToolTip
        ConstantLines   |> Option.iter p.set_ConstantLines
        ConstantBands   |> Option.iter p.set_ConstantBands
        Texts           |> Option.iter p.set_Texts
        YAxes           |> Option.iter p.set_YAxes
        XLowerMargin    |> Option.iter p.set_XLowerMargin
        XUpperMargin    |> Option.iter p.set_XUpperMargin
        XAutoRange      |> Option.iter p.set_XAutoRange
        XLowerBound     |> Option.iter p.set_XLowerBound
        XUpperBound     |> Option.iter p.set_XUpperBound
        LogX            |> Option.iter p.set_LogX
        LogY            |> Option.iter p.set_LogY
        TimeZone        |> Option.iter p.set_TimeZone
        Crosshair       |> Option.iter p.set_Crosshair
        fun gfx -> p <|> gfx

    static member Plot(gfx: XYGraphics, ?Width, ?Height, ?Title, ?XLabel, ?YLabel, ?ShowLegend, ?UseToolTip, ?ConstantLines, ?ConstantBands, ?Texts, ?YAxes, ?XLowerMargin, ?XUpperMargin, ?XAutoRange, ?XLowerBound, ?XUpperBound, ?LogX, ?LogY, ?TimeZone, ?Crosshair) = 
        gfx |> BeakerChartBeta.Plot(?Width = Width, ?Height = Height, ?Title = Title, ?XLabel = XLabel, ?YLabel = YLabel, ?ShowLegend = ShowLegend, ?UseToolTip = UseToolTip, ?ConstantLines = ConstantLines, ?ConstantBands = ConstantBands, ?Texts = Texts, ?YAxes = YAxes, ?XLowerMargin = XLowerMargin, ?XUpperMargin = XUpperMargin, ?XAutoRange = XAutoRange, ?XLowerBound = XLowerBound, ?XUpperBound = XUpperBound, ?LogX = LogX, ?LogY = LogY, ?TimeZone = TimeZone, ?Crosshair = Crosshair)
