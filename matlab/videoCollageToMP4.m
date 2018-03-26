function videoCollageToMP4(v1name, v2name, vjname, lowVal, highVal, numFrames, fps, scale)
% v1 is the LEFT eye's video, v2 is the RIGHT eye's video
tic

v1 = VideoReader(v1name);
v2 = VideoReader(v2name);

%vj = VideoWriter(vjname, 'Grayscale AVI');
vj = VideoWriter(vjname, 'MPEG-4');
vj.FrameRate = fps;
open(vj);

% Counters
currFrame = 0;

if numFrames == 0 || numFrames > v1.NumberOfFrames
    numFrames = v1.NumberOfFrames;
end

v1 = VideoReader(v1name); % Reopen because Matlab is lame

% Read in the actions file, and label where the stimulus was, and what the
% response was, in the processed video.

while currFrame < numFrames
    im1 = readFrame(v1);
    im2 = readFrame(v2);

    % Adjust contrast
    im1 = imadjust(im1, [lowVal, highVal]);
    im2 = imadjust(im2, [lowVal, highVal]);
    
    % Adjust size
    im1 = imresize(im1, scale);
    im2 = imresize(im2, scale);
    
    join = cat(2, im2, im1);
    writeVideo(vj, join);
    
    currFrame = currFrame + 1;
end

close(vj);

toc

end