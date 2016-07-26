# KinectMotionCapture

[![DOI](https://zenodo.org/badge/doi/10.5281/zenodo.58077.svg)](http://dx.doi.org/10.5281/zenodo.58077)

A simple software for capturing human body movements using the Kinect camera. The software can seamlessly save joints and bones positions for further analysis.

## Features

- Compliance with one Kinect camera connected.
- Tracking up to two people captured on the video stream.
- Indicating by color which joints and bones are fully tracked or inferred.
- Recording body movements of one person. Bones and joints positions data is saved to files.
- Adjusting joint filtering options.
- Saving screenshots of current video stream.

The application recognizes only joints and bones that were fully tracked or inferred by the Kinect camera. Tracked joints and bones with both of their joints tracked are indicated by green color, inferred joints and bones with only one of their joints tracked are indicated by yellow color, and bones with both of their joint inferred are indicated by red color.

Although being capable of tracking up to two skeletons on the video stream, the application saves all positions as if there was only one skeleton source. Therefore, if more than one skeleton is tracked, the user should indicate the main body to be captured by using the Set body function.

For a general overview of the Kinect skeletal tracking system please refer to [1].


## Functions


### Set body

The Set body function allows to choose the body to be captured (and its position saved) from all other bodies present on the video stream.
To use this feature, the person to be captured must stand the closest to the camera, and then the Set body button must be clicked.

### Kinect smoothing parameters

The skeletal tracking joint information can be adjusted across different frames to minimize jittering and stabilize the joint positions over time. This can be done by adjusting the smoothing parameters. A comprehensive description of these options can be found at [1].

### Recording

Body movement can be recorded by clicking the Start recoding button. All data recorded is saved as comma-separated files in “data” folder in the root directory of the application. For the data file to be saved the Stop recording button must be clicked afterwards.
Joints positions are saved as files named “<timestamp>-joint-<joint type>.csv”. The files include data columns which contain timestamp of a measurement (timestamp), joint x position (x), joint y position (y), joint z position (z), and coordinate type (coord_type), which indicates whether the joint was fully tracked (1) or inferred (2).
Bones positions are saved as files named “<timestamp>-bone-<start joint>-<end joint>.csv”. The files include data columns which contain timestamp of a measurement (timestamp), bone absolute rotation matrix (abs_m11 to abs_m44), bone absolute rotation quaternion (abs_x, abs_y, abs_z, and abs_w), bone hierarchical rotation matrix (h_m11 to h_m44), bone hierarchical rotation quaternion (h_x, h_y, h_z, and h_w), and coordinate type (coord_type), which indicates whether both joints of the bone were fully tracked (1), both were inferred (2) or only one of them was tracked (3).

## Requirements
- .NET Framework 4.5.2
- Kinect for Windows SDK v1.8

## References

[1] https://msdn.microsoft.com/en-us/library/hh973074.aspx
