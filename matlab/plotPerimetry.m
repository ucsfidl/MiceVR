function plotPerimetry(actionFileDirectory)
% This script takes in 1 or more action logs and outputs a figure and data
% file showing the accuracy at each position, in a greyscale gradient,
% where white is 100% and black is 0%.
% 
% One graph will be produced for each scale observed.  Each graph will
% encompass the full 180 degrees of azimuthal view.
% 
% In the first version, the logs will have 4 scales: full, 40x40, 20x20 and
% 10x10.

% First, find all the filenames to read in

l = dir([actionFileDirectory '/*.txt']) % Get all mat files, and use that to construct filenames for video files

% Next, read in each line of each file, determine the scale, and then add
% the trial either as correct or incorrect.
for i=1:length(l)
    
end

end