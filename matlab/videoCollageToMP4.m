function videoCollageToMP4(v1name, v2name, vjname, numFrames, fps, vals, scale)
% v1 is the LEFT eye's video, v2 is the RIGHT eye's video
%
% lowVal = 0
% highVal = 0.25 if light not bright, 1 if many pixels saturate (current
% config)
% fps = 60
% scale = 0.5 (down to 200 x 150)
tic

v1 = VideoReader(v1name);
v2 = VideoReader(v2name);

%vj = VideoWriter(vjname, 'Grayscale AVI');
vj = VideoWriter(vjname, 'MPEG-4');
vj.FrameRate = fps;
open(vj);

% Counters
currFrame = 0;

lowVal = vals(1);
highVal = vals(2);

if numFrames == 0 || numFrames > v1.NumberOfFrames
    numFrames = v1.NumberOfFrames;
end

v1 = VideoReader(v1name); % Reopen because Matlab is lame

% Read in the actions file, and label where the stimulus was, and what the
% response was, in the processed video.

while currFrame < numFrames
    %disp(currFrame);
    im1 = readFrame(v1);
    im2 = readFrame(v2);

    % Adjust contrast
    im1 = imadjust(im1, [lowVal, highVal]);
    im2 = imadjust(im2, [lowVal, highVal]);
    
    % Adjust size
    im1 = imresize(im1, scale);
    im2 = imresize(im2, scale);
    
    join = cat(2, im2(:,:,1), im1(:,:,1));
    writeVideo(vj, join);
    
    currFrame = currFrame + 1;
end

close(vj);

beep;

toc

end