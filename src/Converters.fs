namespace Thoth.Json.Net

module Converters =

    open Thoth.Json.Net
    open Newtonsoft.Json
    open Newtonsoft.Json.Linq

    type Converter (?isCamelCase : bool, ?extra: ExtraCoders) =
        inherit JsonConverter()

        override __.CanConvert(_t) = true

        override __.WriteJson(writer, value, _serializer) =
            let t = value.GetType ()
            let encoder = Encode.Auto.LowLevel.generateEncoderCached(t, ?isCamelCase=isCamelCase, ?extra=extra)
            (encoder value).WriteTo(writer)
            writer.Flush()

        override __.ReadJson(reader, t, _existingValue, _serializer) =
            let decoder = Decode.Auto.LowLevel.generateDecoderCached(t, ?isCamelCase=isCamelCase, ?extra=extra)
            let json  = JToken.Load(reader).ToString()
            Decode.unsafeFromString decoder json
