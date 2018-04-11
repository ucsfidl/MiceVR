function trackPupils(collageFileName, frameLim, fps, otsuWeight, pupilSzRangePx, seSize, degPerPx, useGPU)
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
% NEW - single illuminator above
% otsuweight = 0.34      BAD = [0.5 0.47 0.44] Cryo_117
%                        BAD = [0.4 0.35] Berlin_001
%                        BAD = [0.3], GOOD [0.35] Alpha_133
% maxPupilAspectRatio = 1.5   Used to detect eye blinks and to ignore
% candidate pupil

% OLD
% With imopen: 0.5 otsuweight is good with minSize = 200, [0.497vid-0.3] are
% bad, with Cryo - but 0.5 is bad with Candy, but 0.492 is fine
% minPupilSize = 140 also bad with 0.5 otsuweight.
% Cryo - minSize = 50 is good, 70 or higher is bad
%        maxSize = 1500 is good?  1200 too small for Candy's dilation. 
%      otsuWeight = .5 is good, .55 is bad, .45 is bad
% OLD: [0.38 0.4] is good, 0.45 is bad! 0.35 bad unless imopen is used
% minPupilSzPx = 140 
% seSize = 10 hides whiskers/eye lashes, 5 does not!

tic

maxPupilAspectRatio = 1.5;  % 1.437; 1.45 lets some through, so go smaller, but not less than 1.4; motion blur causes 1.4326

% Wash to extend blanks out by 1 on either side?

sizeWt = 0.25; % Cryo 112 - 0.2 has some occasional failures
distWt = 10;
solWt = 1;
distFudge = 0.001;
trialStartOffset = 1;  % Add this much to the recorded trial frame starts

pupilPosHistLen = 40;  % Keep track of the pupil over N frames

trialColor = [0.8 0.8 0.8];
leftColor = [1 1 0.79];  % off-yellow
rightColor = [0.81 1 0.81];  % off-green

frameStart = frameLim(1);
frameStop = frameLim(2);

vin = VideoReader(collageFileName);
relFrame = 1;

if frameStop == 0 || frameStop > vin.NumberOfFrames
    frameStop = vin.NumberOfFrames;
end

numFrames = frameStop - frameStart + 1;
vin = VideoReader(collageFileName); % Reopen because Matlab is lame
vin.CurrentTime = frameStart * 1/fps - 1/fps;

% Don't worry about Matlab log, as it only holds 1000 records, so it is
% pretty much useless for my purposes.
%load([collageFileName(1:end-4) '.mat'], 'log');

% Init storage variables
centers = zeros(numFrames, 2, 2); % Z dimension is 1 for each eye, left eye first
areas = zeros(numFrames, 1, 2);  % Z dimension is 1 for each eye, left eye first

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

while relFrame + frameStart <= frameStop + 1
    %disp(relFrame + frameStart - 1);  %% UNCOMMENT to see status
    im = readFrame(vin);
    for i=1:2  % 1 is L, 2 is R
        if (i == 1)
            imLR(:,:,:,i) = im(:, (size(im,2)/2 + 1):end, :);
        else
            imLR(:,:,:,i) = im(:, 1:(size(im,2)/2), :);
        end
        
        %%%%% CORE ALGORITHM FOR FINDING PUPIL %%%%%%%%%%%%
        % It is important to to binarize before opening to keep parts of
        % the pupil that span an eyelash together.
        subIm = imbinarize(imLR(:,:,:,i), otsuWeight*graythresh(imLR(:,:,:,i)));
        
        % If you want to make this algorithm runnable in realt-time (60Hz), 
        % remove the imopen command by setting seSize arg to 0.
        if (seSize > 0)
            se = strel('disk', seSize);
            if (useGPU)
                subIm = imopen(gpuArray(subIm), se);  % This is slow, and takes about 3 sec per second of video (60 frames)
                subIm = gather(subIm);
            else
                subIm = imopen(subIm, se);  % This is slow, and takes about 3 sec per second of video (60 frames)
            end
        else
            subIm = imLR(:,:,:,i);
        end
            
        % Need to complement, so that pupil is white instead of black;
        % Matlab treats white as foreground and black as background.
        subIm = imcomplement(subIm(:,:,1));
        
        cc = bwconncomp(subIm);
        % For debugging:
        if (relFrame == 91 && i == 2)
            a = 0;
        end
        if (~isempty (cc.PixelIdxList))
            s = regionprops(subIm, {'Centroid', 'MajorAxisLength', 'MinorAxisLength', ...
                'Orientation', 'ConvexArea', 'Solidity'});
            s = s([s.ConvexArea] > pupilSzRangePx(1) & [s.ConvexArea] < pupilSzRangePx(2));
            if (~isempty(s))
                cA = [s.ConvexArea];
                solidity = [s.Solidity];
                if (relFrame > 1)
                    curPos = cat(1, s.Centroid);
                    if (relFrame-1 > pupilPosHistLen)
                        startFr = relFrame - pupilPosHistLen;
                    else
                        startFr = 1;
                    end
                    c = centers(startFr:relFrame-1, :, i);
                    c = c(~isnan(c));
                    cNoNaN = reshape(c, numel(c)/2, 2);
                    avgPrevPos = mean(cNoNaN, 1);
                    prevPos = repmat(avgPrevPos, size(curPos, 1), 1);
                    dist = sqrt(power(curPos(:,1) - prevPos(:,1),2) + power(curPos(:,2) - prevPos(:,2),2))';
                else
                    dist = ones(1,length(s));
                end
                %centroids = [s.Centroid];
                %roundy = [s.MajorAxisLength; s.MinorAxisLength]; 
                %roundy = roundy(1,:) - roundy(2,:);
                % Do not rely on roundy because it gives good signal 
                % if noise is splotchy in just the right way.
                [~,idx] = max( ...
                               solWt * solidity/max(solidity) + ...
                               distWt * min(dist) ./ (dist+distFudge) + ...
                               sizeWt * cA/max(cA));
                if (~isempty(idx))
                    % When the mouse blinks, the aspect ratio gets less
                    % circular and more elliptical.  This tests for that.
                    if (s(idx).MajorAxisLength / s(idx).MinorAxisLength < maxPupilAspectRatio)
                        centers(relFrame,:,i) = s(idx).Centroid; % raw pixel position
                        areas(relFrame,:,i) = s(idx).ConvexArea;  % in px - need to calibrate
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
                    else
                        centers(relFrame,:,i) = [NaN NaN];
                        areas(relFrame,:,i) = [NaN];
                    end
                else
                    centers(relFrame,:,i) = [NaN NaN];
                    areas(relFrame,:,i) = [NaN];
                end
            else
                centers(relFrame,:,i) = [NaN NaN];
                areas(relFrame,:,i) = [NaN];
            end
        else
            centers(relFrame,:,i) = [NaN NaN];
            areas(relFrame,:,i) = [NaN];
        end
    end
    
    join = cat(2, imLR(:,:,:,2), imLR(:,:,:,1));
    writeVideo(vout, join);    

    relFrame = relFrame + 1;
end

% Process the position changes for plotting later
% First, find the central position of the eye, given all of the data, and
% subtract that away.
elavCenter = nanmean(centers(:, 2, :));
elavDeg = -((centers(:,2,:) - elavCenter) .* degPerPx);
elavDeg = reshape(elavDeg, size(elavDeg, 1), size(elavDeg, 3));
azimCenter = nanmean(centers(:, 1, :));
azimDeg = -((centers(:,1,:) - azimCenter) .* degPerPx);
azimDeg = reshape(azimDeg, size(azimDeg, 1), size(azimDeg, 3));

% If trial times are available, incorporate into the graphs
load([collageFileName(1:end-4) '.mat'], 'trialStarts', 'trialEnds');
trialStartFrames = [trialStarts.FrameNumber] + trialStartOffset;
trialStartFrames = trialStartFrames(trialStartFrames <= frameStop);  % Get rid of extras
trialStartFrames = trialStartFrames(trialStartFrames >= frameStart);


%{
% Old code for reading the actions file - inaccurate so DO NOT USE
trialFile = fopen([collageFileName(1:end-4) '_actions.txt']);
if (trialFile ~= -1) % File found
    fgetl(trialFile);  % First line is a header so ignore
    expr = '.*?\t.*?\t(.*?)\t'; % The last frame of each trial is in the 3rd column in the actions file
    while(true)
        line = fgetl(trialFile);
        if (line ~= -1) % The file is not empty
            tokens = regexp(line, expr, 'tokens');
            startFrame = str2double(tokens{1}{1}); %- power(2, (numel(trialStartFrames)));
            if startFrame > frameStop
                break;
            elseif startFrame >= frameStart
                trialStartFrames = [trialStartFrames startFrame-frameStart + 1];
            end
        else
            break;
        end
    end
end
%}


save([collageFileName(1:end-4) '_ann.mat'], 'centers', 'areas', 'elavDeg', 'azimDeg', 'trialStartFrames', 'trialEndFrames', 'stimLocs', 'actionLocs');

ymin = -25;
ymax = 25;

% Plot both eyes
figure; hold on
if (~isempty(trialStartFrames))
    plot(cat(1, trialStartFrames, trialStartFrames), [ymin ymax], 'LineWidth', 1, 'Color', trialColor);
end
h = plot(frameStart:frameStop, elavDeg(:, 1), 'r', frameStart:frameStop, elavDeg(:, 2), 'b');
title([collageFileName(1:end-4) ': Pupil Elevation'], 'Interpreter', 'none');
ylabel('elevation rel. to first frame (deg, + right, - left)');
xlabel('frame #');
legend(h, 'left eye', 'right eye');
ylim([ymin ymax]);

figure; hold on
if (~isempty(trialStartFrames))
    plot(cat(1, trialStartFrames, trialStartFrames), [ymin ymax], 'LineWidth', 1, 'Color', trialColor);
end
h = plot(frameStart:frameStop, azimDeg(:, 1), 'r', frameStart:frameStop, azimDeg(:, 2), 'b');
title([collageFileName(1:end-4) ': Pupil Azimuth'], 'Interpreter', 'none');
ylabel('azimuth rel. to first frame (deg, + right, - left)');
xlabel('frame #');
legend(h, 'left eye', 'right eye');
ylim([ymin ymax]);

close(vout);

toc

beep

end