namespace Thoth.Json.Net

open System.Diagnostics

module Converters =

    open Thoth.Json.Net
    open Newtonsoft.Json
    open Newtonsoft.Json.Linq
    open FSharp.Reflection

    type Converter (?isCamelCase : bool, ?extra: ExtraCoders) =
        inherit JsonConverter()

        override __.CanConvert(t) =
                t.Name = "FSharpOption`1" && t.Namespace = "Microsoft.FSharp.Core" //OPtion
                || FSharpType.IsUnion t && t.Name <> "FSharpList`1"   && t.Name <> "FSharpOption`1" //Union
                || FSharpType.IsTuple t //Tuple
                || t.Name = "FSharpMap`2"  && t.Namespace = "Microsoft.FSharp.Collections" //Map
                || t.IsEnum //enum

        override __.WriteJson(writer, value, _serializer) =
            let t = value.GetType ()
            let encoder = Encode.Auto.LowLevel.generateEncoderCached(t, ?isCamelCase=isCamelCase, ?extra=extra)
            (encoder value).WriteTo(writer)
            writer.Flush()

        override __.ReadJson(reader, t, _existingValue, _serializer) =
            let decoder = Decode.Auto.LowLevel.generateDecoderCached(t, ?isCamelCase=isCamelCase, ?extra=extra)
            let json  = JToken.Load(reader).ToString()
            Decode.unsafeFromString decoder json

