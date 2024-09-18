# VR renderer of animated point-clouds
This is the official implementation of Virtual Reality for Immersive Education in Orthopedic Surgery Digital Twins.

## [Project page](https://jonashein.github.io/surgerydigitization/) | [Paper](https://arxiv.org/abs/2409.11014)
[Jonas Hein](https://scholar.google.com/citations?user=Kk_o9AYAAAAJ), [Jan Grunder](https://github.com/Klark007), [Lilian Calvet](https://scholar.google.com/citations?user=6JewdrMAAAAJ), [Fédéric Giraud](https://scholar.google.com/citations?user=Lf6jqg4AAAAJ), [Nicola Alessandro Cavalcanti](https://scholar.google.com/citations?user=ulEV9OkAAAAJ), [Fabio Carrillo](https://scholar.google.com/citations?user=n7A302IAAAAJ), [Philipp Fürnstahl](https://scholar.google.com/citations?user=nQ4B3BgAAAAJ)

## Download
After cloning this repository, download the point cloud animation data from [here](https://polybox.ethz.ch/index.php/s/J3afMWTWKIrkXIr).
Extract the archive into the pointclouds directory. The final directory structure should look as follows:
```
SurgTwinVR/Assets/pointclouds/animation/004140.ply
```


## Installation
1. Install Unity 2022.3.28f1 with Android Build Support, OpenJDK, Android SDK & NDK Tools
2. Run using either Meta XR Simulator or via Meta Quest Link on physical hardware

## Features
- Basic VR Locomotion and interaction with a medical drill
- Real time rendering of dynamic Point clouds based on the [method from Markus Schütz](https://arxiv.org/abs/2104.07526)
  - Rasterization using a Compute Shader
  - Executed as custom post-processing step in the Universal Render Pipeline
- Asynchronous loading of animation data

##
<p align="center">
  <img src="https://github.com/Klark007/SurgTwinVR/blob/main/media/Distant%20Sim.png" />
</p>
<p align="center">
  <img src="https://github.com/Klark007/SurgTwinVR/blob/main/media/Assistant%20Sim.png" />
</p>
<p align="center">
  <img src="https://github.com/Klark007/SurgTwinVR/blob/main/media/User%20Real.png" />
</p>


## Documentation
For further details see ["Technical overview.pdf"](https://github.com/Klark007/SurgTwinVR/blob/main/Technical%20overview.pdf).

## Citation
```
@InProceedings{Hein_2024_ISMAR,
  author    = {Hein, Jonas and Grunder, Jan and Calvet, Lilian and Giraud, Fr\'ed\'eric and Cavalcanti, Nicola Alessandro and Carrillo, Fabio and F\"urnstahl, Philipp},
  booktitle = {2024 IEEE International Symposium on Mixed and Augmented Reality Adjunct (ISMAR-Adjunct)},
  title     = {Virtual Reality for Immersive Education in Orthopedic Surgery Digital Twins},
  year      = {2024},
  volume    = {},
  number    = {},
  pages     = {},
  keywords  = {Virtual Reality;Surgery Digital Twin;Surgical Training;Medical Simulation;Spinal Surgery},
}
```

## Acknowledgement
The VR app was created during a stay at the Research in Orthopedic Computer Science Group (ROCS) at Balgrist Hospital Zurich. This is a demo submission to ISMAR 2024 "Virtual Reality for Immersive Education in Orthopedic Surgery Digital
Twins". This work has been supported by the OR-X - a swiss national research infrastructure for translational surgery - and associated funding by the University of Zurich and University Hospital Balgrist.
