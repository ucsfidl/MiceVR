function videoCollageAllAVIToMP4(deleteOriginals)
% Batch script run at the end of each day to take eye tracking videos and
% compress them as low resolution MP4s.
%
% Relies on Parallel Computing Toolbox!

list1 = dir('*1.avi'); % right eye videos
list2 = dir('*2.avi'); % left eye videos

fps = 60;
scale = 0.25;
lowVal = 0;
highVal = 0.25;
numFrames = 0;  % 0 means do all frames in the video


if length(list1) ~= length(list2)
    error('You do not have the same number of left and right eye videos.  Please investigate!');
end

parfor i=1:length(list1)
    videoCollageToMP4(list2(i).name, list1(i).name, ...
        [list1(i).name(1:end-6), '.mp4'], lowVal, highVal, numFrames, fps, scale);
    
    if (deleteOriginals)
        delete(list2(i).name);
        delete(list1(i).name);
    end
end

end