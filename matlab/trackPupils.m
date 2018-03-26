function trackPupils(collageFile)
% This script will analyze a video file containing both eyes, the right eye
% on the left side and the left eye on the right side of the video. 
% It then saves several outputs:
%  - The centroids of each pupil over time
%  - The size of each pupil over time
%  - The deviation in elevation of the pupil over time (extracted from
%  centroids - PCA?)
%  - The deviation in azimuth of the pupil over time
%  - A list of trial boundary times
%  - A list of eye blinks over time
% It will also output graphs showing the deviation in azimuth over time for both eyes,
% the deviation in elevation over time for both eyes, and the deviation in
% both pupil sizes over time (5 traces per session).  The traces will have
% shading to indicate different trials.
% This program also produces an annotated videos in which the pupils are
% outlined.

vin = VideoReader(collageFile);

% The collage file has 2 videos in it: right eye on left and left eye on
% right.  So split the image in 2 to analyze, in parallel.

% Step 1 - break up the image

[c r m] = imfindcircles(im1r, [15 36], 'ObjectPolarity', 'dark', ...
    'Method', 'phasecode', 'EdgeThreshold', 0)
viscircles(c, r, 'Color', 'blue', 'LineWidth', 1, 'EnhanceVisibility', false);



end