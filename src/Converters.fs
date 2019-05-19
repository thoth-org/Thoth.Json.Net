module Thoth.Json.Net.Converters

open Thoth.Json.Net
open Newtonsoft.Json
open Newtonsoft.Json.Linq

type Converter (?isCamelCase : bool, ?extra: ExtraCoders) =
    inherit JsonConverter()

    override __.CanConvert(t) = true

    override __.WriteJson(writer, value, _) = 
        let t = value.GetType ()
        let encoder = Encode.Auto.generateEncoderCached(t, ?isCamelCase=isCamelCase, ?extra=extra)
        (encoder value).WriteTo(writer)
        writer.Flush()
        
    override __.ReadJson(reader, t, _, _) =
        let decoder = Decode.Auto.generateDecoderCached(t, ?isCamelCase=isCamelCase, ?extra=extra)
        let json  = JToken.Load(reader).ToString()
        Decode.unsafeFromString decoder json 