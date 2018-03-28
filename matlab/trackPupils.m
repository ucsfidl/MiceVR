function trackPupils(collageFileName, frameLim, fps, otsuWeight, minPupilSzPx, seSize, useGPU)
% This script will analyze a video file containing both eyes, the right eye
% on the left side and the left eye on the right side of the video. 
% It then saves several outputs:
%  - The centroids of each pupil over time
%  - The size of each pupil over time (pixel area)
%  - The deviation in elevation of the pupil over time
%  - The deviation in azimuth of the pupil over time
%  - A list of trial boundary times
%  - A list of eye blinks over time?
% It will also output graphs showing the deviation in azimuth over time for both eyes,
% the deviation in elevation over time for both eyes, and the deviation in
% both pupil sizes over time (5 traces per session).  The traces will have
% shading to indicate different trials.
% This program also produces an annotated video in which the pupils are
% outlined.

% Good settings for arguments:
% otsuWeight = [0.38 0.4] is good, 0.45 is bad! 0.35 bad unless imopen is used
% minPupilSzPx = 100
% seSize = 10 hides whiskers/eye lashes, 5 does not!

tic

frameStart = frameLim(1);
frameStop = frameLim(2);

vin = VideoReader(collageFileName);
currFrame = frameStart;

if frameStop == 0 || frameStop > vin.NumberOfFrames
    frameStop = vin.NumberOfFrames;
end

numFrames = frameStop - frameStart + 1;
vin = VideoReader(collageFileName); % Reopen because Matlab is lame
vin.CurrentTime = currFrame * 1/fps - 1/fps;

load([collageFileName(1:end-4) '.mat'], 'log');

% Init storage variables
centers = zeros(numFrames, 2, 2); % Z dimension is 1 for each eye, left eye first
areas = zeros(numFrames, 1, 2);  % Z dimension is 1 for each eye, left eye first
elavDeltas = zeros(numFrames, 2); % x dimension is 1 for each eye, left eye first
azimDeltas = zeros(numFrames, 2); % x dimension is 1 for each eye, left eye first

if (seSize > 0)
    vout = VideoWriter([collageFileName(1:end-4) '_opened_ann.mp4'], 'MPEG-4');
else
    vout = VideoWriter([collageFileName(1:end-4) '_ann.mp4'], 'MPEG-4');    
end
vout.FrameRate = fps;
open(vout);

imLR = zeros(vin.Height, vin.Width/2, vin.BitsPerPixel/8, 2, 'uint8'); % 2 RGB images per video frame

% The collage file has 2 videos in it: right eye on left and left eye on
% right.  So split the image in 2 to analyze, in parallel, then stitch back
% together with annotation for confirmation of accurate tracking.

% Also, some bouncing motion needs to be accounted for with frame
% registration.

while currFrame <= frameStop
    %disp(currFrame);  %% UNCOMMENT to see status
    im = readFrame(vin);
    for i=1:2  % 1 is L, 2 is R
        if (i == 1)
            imLR(:,:,:,i) = im(:, (size(im,2)/2 + 1):end, :);
        else
            imLR(:,:,:,i) = im(:, 1:(size(im,2)/2), :);
        end
        
        %%%%% CORE ALGORITHM FOR FINDING PUPIL %%%%%%%%%%%%
        % Remove the imopen command to make real-time by setting seSize arg to 0 
        if (seSize > 0)
            se = strel('disk', seSize);
            if (useGPU)
                subIm = imopen(imLR(:,:,:,i), se);  % This is slow, and takes about 3 sec per second of video (60 frames)
            else
                subIm = imopen(gpuArray(imLR(:,:,:,i)), se);  % This is slow, and takes about 3 sec per second of video (60 frames)
                subIm = gather(subIm);
            end
        else
            subIm = imLR(:,:,:,i);
        end
            
        subIm = imbinarize(subIm, otsuWeight*graythresh(subIm));
        subIm = imcomplement(subIm(:,:,1));
        cc = bwconncomp(subIm);
        if (~isempty (cc.PixelIdxList))
            numPixels = cellfun(@numel, cc.PixelIdxList);
            idx = find(numPixels > minPupilSzPx);
            %[~,idx] = max(numPixels);
            if (~isempty(idx))
                subIm = ismember(labelmatrix(cc), idx);
                s = regionprops(subIm, {'Centroid', 'MajorAxisLength', 'MinorAxisLength', ...
                    'Orientation', 'ConvexArea' });
                roundy = [s.MajorAxisLength; s.MinorAxisLength]; 
                roundy = roundy(1,:) - roundy(2,:);
                [~,idx] = min(roundy);
                % Write variables to a data file, and also plot them at the end
                % if desired.
                centers(currFrame,:,i) = s(idx).Centroid; % raw pixel position
                areas(currFrame,:,i) = s(idx).ConvexArea;  % in px - need to calibrate
                %%% DRAW ONTO VIDEO FOR VALIDATION %%%
                c = s(idx).Centroid;
                rMaj = s(idx).MajorAxisLength / 2;
                rMin = s(idx).MinorAxisLength / 2;
                angle = -s(idx).Orientation;
                dxMaj = rMaj * cosd(angle);
                dyMaj = rMaj * sind(angle);
                dxMin = rMin * cosd(angle+90);
                dyMin = rMin * sind(angle+90);
                lines = [c(1)-dxMaj, c(2)-dyMaj, c(1)+dxMaj, c(2)+dyMaj;
                         c(1)-dxMin, c(2)-dyMin, c(1)+dxMin, c(2)+dyMin];
                imLR(:,:,:,i) = insertShape(imLR(:,:,:,i), 'line', lines, ...
                    'LineWidth', 2, 'Color', 'red');
            end
        else
            centers(currFrame,:,i) = [NaN NaN];
            areas(currFrame,:,i) = [NaN];
        end
    end
    
    join = cat(2, imLR(:,:,:,2), imLR(:,:,:,1));
    writeVideo(vout, join);    

    currFrame = currFrame + 1;
end

% Process the elevation change and plot
for t=1:length(centers)
    for i=1:2  % Once for each eye
        if (t == 1)
            elavDeltas(t, i) = 0;
            azimDeltas(t, i) = 0;
        else
            elavDeltas(t, i) = centers(t, 2, i) - centers(1, 2, i);
            azimDeltas(t, i) = centers(t, 1, i) - centers(1, 1, i);
        end
    end
end

save([collageFileName(1:end-4) '_ann.mat'], 'log', 'centers', 'areas', 'elavDeltas', 'azimDeltas');

% left eye for now
figure;
plot(1:length(elavDeltas), elavDeltas(:, :));
title([collageFileName(1:end-4) ': Pupil Elevation']);
legend('left', 'right');
ylim([-20 20]);

figure;
plot(1:length(azimDeltas), azimDeltas(:, :));
title([collageFileName(1:end-4) ': Pupil Azimuth']);
legend('left', 'right');
ylim([-20 20]);

close(vout);

toc

end