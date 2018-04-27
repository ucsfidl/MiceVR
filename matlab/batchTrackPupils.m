function batchTrackPupils(root, frameLim, fps, otsuWeight, pupilSzRangePx, seSize, useGPU)
% This helper traverses a directory, opens a parallel pool, and then tracks
% the pupils of all of the video files in the directory set.
%
% This is an easy way to track all of the pupils in a set of files the day
% after they are recorded.
%
% The files are then copied to their target directory, in which
% analyzePupils.m can then be run after reviewing the videos to confirm the
% tracking was good and not missing too much data.

cd(root);
l = dir('**/*.mat'); % Get all mat files, and use that to construct filenames for video files
parfor i=1:length(l)
    cd(l(i).folder);
    vidNames = {''; ''};  % first name is right vid, second is left
    fileRoot = l(i).name(1:end-4);
    for j=1:2
        m = dir([fileRoot '_' num2str(j) '.mp4']);
        if (~isempty(m))
           vidNames{j} = [m(1).folder '\' m(1).name];
        end
    end
    disp(['Working on ' vidNames{1} ' + ' vidNames{2}]);
    trackPupils(vidNames{2}, vidNames{1}, frameLim, fps, otsuWeight, pupilSzRangePx, seSize, useGPU);
end

% At the end, move all processed videos and results to the regular folder

end