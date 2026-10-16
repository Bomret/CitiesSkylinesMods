# Invariants

Rules the mods may never break. Each entry names the concepts it governs, how it breaks, and how it is
checked.

### INV-0001

Class: lifecycle
Concepts: Feature, level load, scene object
Statement: A feature checks a cached scene object, such as its camera or light, before each use of it in
`OnLoaded` or on enable. A feature that uses no cached scene object in a given `OnLoaded` has nothing to
check there. It checks with Unity's overloaded `== null` and finds the object again when that check is true.
That operator treats an object destroyed by a level load as null. `is null`, `ReferenceEquals`, and `??`
compare the raw reference, so they miss a destroyed object. A feature never skips this check behind state
that survives `OnUnloading`.
Breaks: silently
Check: none — the mods run only inside the game, and Unity's destroyed-object semantics have no test seam
outside the engine.
Symbols: Feature.OnLoaded, Feature.OnUnloading
Paths: mods/*/Features/**
