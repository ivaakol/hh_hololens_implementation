# hh_hololens_implementation

A gaze-contingent simulation of homonymous hemianopia (loss of one half of the visual field) for the Microsoft HoloLens 2, built in Unity. It draws a veil over the blind half of the user's field of view that follows their eyes in real time, so the simulated field loss stays fixed relative to where they look.
Intended as a demo prototype.

*Features*
Gaze-contingent veil: the blind field tracks the user's eye gaze (not just head direction), using the HoloLens 2 eye tracker.
Left or right: simulate left or right homonymous hemianopia with a single toggle.
Optional macular sparing: a small clear disc around the point of fixation, adjustable in degrees, reflecting the preserved central vision many patients retain.
Adjustable boundary: the edge between the seeing and blind field can be made sharp or softly feathered.
Adjustable opacity: control how strongly the veil obscures the blind field.
Neutral veil: the blind field is filled with a plain neutral colour, which can be altered


*Files*
HemiVeil.shader: the veil shader.
HemiVeilDriver_MRTK.cs: gaze driver using MRTK's eye-gaze provider.


*Requirements*
Unity 2022.3 LTS
A HoloLens 2 with eye tracking and UWP build support
OpenXR with eye gaze enabled, and the GazeInput capability ticked
MRTK 2.8