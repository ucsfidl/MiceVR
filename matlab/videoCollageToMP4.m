function videoCollageToMP4(vLeftName, vRightName, vjname, numFrames, fps, vals, scale)
% lowVal = 0
% highVal = 0.25 if light not bright, 1 if many pixels saturate (current
% config)
% fps = 60
% scale = 0.5 (down to 200 x 150)

% videoCollageToMP4('Candy_206_2.avi', 'Candy_206_1.avi', 'Candy_206.mp4', 0, 60, [0, 1], 0.5)

tic

vL = VideoReader(vLeftName);
vR = VideoReader(vRightName);

%vj = VideoWriter(vjname, 'Grayscale AVI');
vj = VideoWriter(vjname, 'MPEG-4');
vj.FrameRate = fps;
open(vj);

% Counters
currFrame = 0;

lowVal = vals(1);
highVal = vals(2);

if numFrames == 0 || numFrames > vL.NumberOfFrames || numFrame > vR.NumberOfFrames
    if (vR.NumberOfFrames ~= vL.NumberOfFrames)
        error('Videos are not same length, so something went wrong with syncing!  Aborting.');
    end
    numFrames = vL.NumberOfFrames;
end

% Reopen because Matlab is lame and calls to NumberOfFrames screws up the
% current position in the file.
vL = VideoReader(vLeftName); 
vR = VideoReader(vRightName);

% Read in the actions file, and label where the stimulus was, and what the
% response was, in the processed video.

while currFrame < numFrames
    %disp(currFrame);
    imL = readFrame(vL);
    imR = readFrame(vR);

    % Adjust contrast
    imL = imadjust(imL, [lowVal, highVal]);
    imR = imadjust(imR, [lowVal, highVal]);
    
    % Adjust size
    imL = imresize(imL, scale);
    imR = imresize(imR, scale);
    
    join = cat(2, imR(:,:,1), imL(:,:,1));
    writeVideo(vj, join);
    
    currFrame = currFrame + 1;
end

close(vj);

beep;

toc

end