# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 3.5.0 - 2019-06-24
### Changed

* Stop using first person when reporting an error. Related to https://github.com/thoth-org/Thoth.Json/issues/19

## 3.5.0-beta-001 - 2019-06-07
### Changed

* Use `paket.template` to generate the nupkg. The goal is to have "correct" dependencies handling

## 3.4.0 - 2019-06-07
### Added

* `Decode.Auto.LowLevel` providing better interop when consuming Thoth.Json.Net from a C# project
* `Encode.Auto.LowLevel` providing better interop when consuming Thoth.Json.Net from a C# project
* (Json)Converter, this is needed for people using Asp.Net WebApi (by @SCullman)

### Fixed

* Fix #11: Replicate Fable behaviour when encoding `StringEnum`. Only when not on NETFRAMEWORK!!!

### Changed

* Ensure that Thoth.Json.Net references the minimum allowed version of Newtonsoft.Json (by @bentayloruk)

## 3.3.0 - 2019-06-05
### Fixed

* Fix #12: Support auto coders with recursive types (by @alfonsogarciacaro)

## 3.2.0 - 2019-05-14
### Fixed

* Fix #13: Decode.string fails on strings with datetime

## 3.1.0 - 2019-05-03
### Fixed

* Fix auto encoder when generating an optinal unkown type and the runtime value is `None`

## 3.0.0 - 2019-04-17
### Changed

* Release stable version

## 3.0.0-beta-005 - 2019-04-10
### Changed

* Lower requested version of Newtonsoft to `>=11.0.2`

## 3.0.0-beta-004 - 2019-04-10

## 3.0.0-beta-003 - 2019-04-02
### Fixed
* Fix Decode.oneOf in combination with object builder (by @alfonsogarciacaro)
* Make `Decode.field` consistant and report the exact error
* Make `Decode.at` bconsistant and report the exact error

### Changed
* Make Decode.object output 1 error or all the errors if there are severals
* Improve BadOneOf errors readibility

## 3.0.0-beta-002 - 2019-01-11
### Added

* Adding `TimeSpan` support (by @rfrerebe)

## 3.0.0-beta-001 - 2019-01-10
### Added

* Add `Set` support in auto coders (by @alfonsogarciacaro)
* Add `extra` support to auto coders. So people can now override/extends auto coders capabilities (by @alfonsogarciacaro)

### Changed

* Use reflection for auto encoders just as auto decoders. This will help keep the JSON representatin in synx between manual and auto coders (by @alfonsogarciacaro)
* `Decode.datetime` always outputs universal time (by @alfonsogarciacaro)
* If a coder is missing, auto coders will fail on generation phase instead of coder evaluation phase (by @alfonsogarciacaro)
* By default `int64` - `uint64` - `bigint` - `decimal` support is being disabled from auto coders to reduce bundle size (by @alfonsogarciacaro)

### Removed

* Mark `Decode.unwrap` as private. It's now only used internally for object builder. This will encourage people to use `Decode.fromValue`.

## 2.5.0 - 2018-11-08
### Added

* Make auto decoder support record/unions with private constructors

## 2.4.0 - 2018-11-07
### Changed

* Make auto decoder succeeds on Class marked as optional

## 2.3.0 - 2018-10-18
### Changed

* Added CultureInfo.InvariantCulture to all Encoder functions where it was possible (by @draganjovanovic1)

### Fixed

* Fix #59: Make auto decoder support optional fields when missing from JSON
* Fix #61: Support object keys with JsonPath characters when using `Decode.dict`
* Fix #51: Add support for `Raw` decoder in object builders

## 2.2.0 - 2018-10-11
### Added

* Re-add optional and optionalAt related to #51

### Fixed

* Various improvements for Primitive types improvements  (by @draganjovanovic1)
* Fix decoding of optional fields (by @eugene-g)

## 2.1.0 - 2018-10-3
### Fixed

* Fix nested object builder (ex: get.Optional.Field > get.Required.Field)
* Fix exception handling

## 2.0.0 - 2018-10-01
### Added

* Release stable

## 2.0.0-beta-004 - 2018-08-03
### Added

* Add Encoders for all the equivalent Decoders

## 2.0.0-beta-003 - 2018-07-16
### Changed

* Make auto decoder safe by default

## 2.0.0-beta-002 - 2018-07-12
### Fixed

* Fix `Decode.decodeString` signature

## 2.0.0-beta-001 - 2018-07-11
### Added

* Support auto decoders and encoders
* Add object builder style for the decoders

### Changed

* Better error, by now tracking the path

### Deprecated

* Mark `Encode.encode`, `Decode.decodeString`, `Decode.decodeValue` as obsoletes

### Removed

* Remove pipeline style for the decoders

## 1.1.0 - 2018-06-08
### Fixed

* Ensure that `field` `at` `optional` `optionalAt` works with object

## 1.0.1 - 2018-05-30
### Fixed

* A float from int works

## 1.0.0 - 2018-04-17
### Added

* Initial release
