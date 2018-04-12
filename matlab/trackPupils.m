function trackPupils(collageFileName, numStim, frameLim, fps, otsuWeight, pupilSzRangePx, seSize, degPerPx, useGPU)
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
trialEndOffset = 1;

pupilPosHistLen = 40;  % Keep track of the pupil over N frames

trialColor = [0.5 0.5 0.5];
%leftColor = [1 1 0.79];  % off-yellow
%rightColor = [0.81 1 0.81];  % off-green

colorLeftNear = [1 0.87 0.71]; % dull orange
colorLeftFar = [1 1 0.79];  % dull yellow
colorRightNear = [0.84 0.89 0.99];  % dull blue
colorRightFar = [0.84 0.98 0.99]; % dull cyan
colorCenter = [1 0.9 0.99];  % dull purple

if (numStim == 4)
    allColors = [colorLeftNear; colorLeftFar; colorRightNear; colorRightFar];
elseif (numStim == 3)
    allColors = [colorLeftNear; colorRightNear; colorCenter];
end

% Be sure to change these if the x location of the trees changes
stimLeftNear = 19977;
stimLeftFar = 19978;
stimRightNear = 20023;
stimRightFar = 20022;
stimLeft = 19980;
stimRight = 20020;
stimCenter = 20000;

frameStart = frameLim(1);
frameStop = frameLim(2);

vin = VideoReader(collageFileName);
relFrame = 1;

totalFrames = vin.NumberOfFrames;
if frameStop == 0 || frameStop > totalFrames
    frameStop = totalFrames;
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

% Read the actions file so the graph can be properly annotated
stimLocs = [];
actionLocs = [];
actionsFile = fopen([collageFileName(1:end-4) '_actions.txt']);
if (actionsFile ~= -1) % File found
    fgetl(actionsFile);  % First line is a header so ignore
    expr = '.*?\t.*?\t.*?\t.*?\t(.*?)\t.*?\t.*?\t(.*?)\t'; % The last frame of each trial is in the 3rd column in the actions file
    while(true)
        line = fgetl(actionsFile);
        if (line ~= -1) % The file is not finished
            tokens = regexp(line, expr, 'tokens');
            stimLocs = [stimLocs str2num(tokens{1}{1})];
            actionLocs = [actionLocs str2num(tokens{1}{2})];
        else
            break;
        end
    end
end

ymin = -25;
ymax = 25;

% If trial times are available, incorporate into the graphs
load([collageFileName(1:end-4) '.mat'], 'trialStarts', 'trialEnds');
trialStartFrames = [1-trialStartOffset trialStarts.FrameNumber] + trialStartOffset; % First trial start is not written
if (exist('trialEnds', 'var'))
    trialEndFrames = [trialEnds.FrameNumber totalFrames-trialEndOffset] + trialEndOffset; % Last trial end is not written
else
    trialEndFrames = [trialStartFrames(2:end) totalFrames];
end

% If game ended mid-trial, don't color the final trial
if (length(trialStartFrames) > length(stimLocs))
    trialStartFrames = trialStartFrames(1:end-1);
    trialEndFrames = trialEndFrames(1:end-1);
end

stimColors = zeros(1, length(stimLocs), 3);  % Stim  location colors to to use when plotting
actionColors = zeros(1, length(actionLocs), 3);

if (numStim == 4)
    for i=1:length(stimLocs)
        if (stimLocs(i) == stimLeftNear)
            stimColors(1,i,:) = colorLeftNear;
        elseif (stimLocs(i) == stimLeftFar)
            stimColors(1,i,:) = colorLeftFar;
        elseif (stimLocs(i) == stimRightNear)
            stimColors(1,i,:) = colorRightNear;
        elseif (stimLocs(i) == stimRightFar)
            stimColors(1,i,:) = colorRightFar;            
        end
        
        if (actionLocs(i) == stimLeftNear)
            actionColors(1,i,:) = colorLeftNear;
        elseif (actionLocs(i) == stimLeftFar)
            actionColors(1,i,:) = colorLeftFar;
        elseif (actionLocs(i) == stimRightNear)
            actionColors(1,i,:) = colorRightNear;
        elseif (actionLocs(i) == stimRightFar)
            actionColors(1,i,:) = colorRightFar;            
        end
    end
elseif (numStim == 3)
    for i=1:length(stimLocs)
        if (stimLocs(i) == stimLeft)
            stimColors(1,i,:) = colorLeftNear;
        elseif (stimLocs(i) == stimRight)
            stimColors(1,i,:) = colorRightNear;
        elseif (stimLocs(i) == stimCenter)
            stimColors(1,i,:) = colorCenter;
        end
        
        if (actionLocs(i) == stimLeft)
            actionColors(1,i,:) = colorLeftNear;
        elseif (actionLocs(i) == stimRight)
            actionColors(1,i,:) = colorRightNear;
        elseif (actionLocs(i) == stimCenter)
            actionColors(1,i,:) = colorCenter;
        end
    end
end

% Make trial intervals into matrices that can be plotted as patches
trimmedStarts = trialStartFrames(trialStartFrames >= frameStart);
numTruncatedStart = length(trialStartFrames) - length(trimmedStarts); % tells me how much was deleted up front
trimmedStarts = trimmedStarts(trimmedStarts <= frameStop);
numTruncatedEnd = length(trialStartFrames) - length(trimmedStarts) - numTruncatedStart;
if (trimmedStarts(1) > frameStart)
    trimmedStarts = [frameStart trimmedStarts];
    numTruncatedStart = numTruncatedStart - 1;
end
trimmedEnds = trialEndFrames(trialEndFrames <= frameStop);
trimmedEnds = trimmedEnds(trimmedEnds >= frameStart);
if (trimmedEnds(end) < frameStop)
    trimmedEnds = [trimmedEnds frameStop];
end
xTrials = cat(1, trimmedStarts, trimmedEnds);
xTrials = cat(1, xTrials, flipud(xTrials));

yStim = [ymax; ymax; 0; 0];
yStim = repmat(yStim, 1, size(xTrials,2));
yAction = [0; 0; ymin; ymin];
yAction = repmat(yAction, 1, size(xTrials,2));

stimColors = stimColors(1, 1+numTruncatedStart:end-numTruncatedEnd, :);
actionColors = actionColors(1, 1+numTruncatedStart:end-numTruncatedEnd, :);

xPause = cat(1, trimmedEnds(1:end-1), trimmedStarts(2:end));
xPause = cat(1, xPause, flipud(xPause));
yPause = [ymax; ymax; ymin; ymin];
yPause = repmat(yPause, 1, size(xPause,2));

%{
midline = 20000;
leftStimTrialMask = stimLocs < midline;
centerStimTrialMask = stimLocs == midline;
rightStimTrialMask = stimLocs > midline;

% With full trial start and end frame list, extract the trial boundaries
% for each stim location
% LEFT STIM
leftStimTrialStarts = leftStimTrialMask .* trialStartFrames;
leftStimTrialStarts = leftStimTrialStarts(leftStimTrialStarts > 0);
stm = circshift(leftStimTrialMask, 1);
leftStimTrialEnds = stm(2:end) .* trialStartFrames(2:end);
leftStimTrialEnds = leftStimTrialEnds(leftStimTrialEnds > 0);
if (length(leftStimTrialStarts) > length(leftStimTrialEnds))
    leftStimTrialEnds = [leftStimTrialEnds totalFrames];
end
leftStimTrialStarts = leftStimTrialStarts(leftStimTrialStarts >= frameStart);
leftStimTrialEnds = leftStimTrialEnds(leftStimTrialEnds >= frameStart);
if (length(leftStimTrialStarts) < length(leftStimTrialEnds))
    leftStimTrialStarts = [frameStart leftStimTrialStarts];
end
leftStimTrialStarts = leftStimTrialStarts(leftStimTrialStarts <= frameStop);
leftStimTrialEnds = leftStimTrialEnds(leftStimTrialEnds <= frameStop);
if (length(leftStimTrialStarts) > length(leftStimTrialEnds))
    leftStimTrialEnds = [leftStimTrialEnds frameStop];
end

xLeftStim = cat(1, leftStimTrialStarts, leftStimTrialEnds);
xLeftStim = cat(1, xLeftStim, flipud(xLeftStim));
yLeftStim = [ymax; ymax; 0; 0];
yLeftStim = repmat(yLeftStim, 1, size(xLeftStim,2));

% RIGHT STIM
rightStimTrialStarts = rightStimTrialMask .* trialStartFrames;
rightStimTrialStarts = rightStimTrialStarts(rightStimTrialStarts > 0);
stm = circshift(rightStimTrialMask, 1);
rightStimTrialEnds = stm(2:end) .* trialStartFrames(2:end);
rightStimTrialEnds = rightStimTrialEnds(rightStimTrialEnds > 0);
if (length(rightStimTrialStarts) > length(rightStimTrialEnds))
    rightStimTrialEnds = [rightStimTrialEnds totalFrames];
end
rightStimTrialStarts = rightStimTrialStarts(rightStimTrialStarts >= frameStart);
rightStimTrialEnds = rightStimTrialEnds(rightStimTrialEnds >= frameStart);
if (length(rightStimTrialStarts) < length(rightStimTrialEnds))
    rightStimTrialStarts = [frameStart rightStimTrialStarts];
end
rightStimTrialStarts = rightStimTrialStarts(rightStimTrialStarts <= frameStop);
rightStimTrialEnds = rightStimTrialEnds(rightStimTrialEnds <= frameStop);
if (length(rightStimTrialStarts) > length(rightStimTrialEnds))
    rightStimTrialEnds = [rightStimTrialEnds frameStop];
end

xRightStim = cat(1, rightStimTrialStarts, rightStimTrialEnds);
xRightStim = cat(1, xRightStim, flipud(xRightStim));
yRightStim = [ymax; ymax; 0; 0];
yRightStim = repmat(yRightStim, 1, size(xRightStim,2));

% Filter out trials outside of the scope of this analysis
trialStartFrames = trialStartFrames(trialStartFrames <= frameStop);  % Get rid of extras
trialStartFrames = trialStartFrames(trialStartFrames >= frameStart);
if (exist('trialEnds', 'var'))
    trialEndFrames = trialEndFrames(trialEndFrames <= frameStop);  % Get rid of extras
    trialEndFrames = trialEndFrames(trialEndFrames >= frameStart);
end


% Make trial intervals into matrices that can be plotted with the patch
% command.
if (exist('trialEnds', 'var'))
    xPause = cat(1, trialEndFrames(1:length(trialStartFrames)), trialStartFrames);
    xPause = cat(1, xPause, flipud(xPause));
    yPause = [ymax; ymax; ymin; ymin];
else
    xPause = cat(1, trialStartFrames, trialStartFrames);
    yPause = [ymax; ymin];
end
yPause = repmat(yPause, 1, size(xPause,2));
%}

save([collageFileName(1:end-4) '_ann.mat'], 'centers', 'areas', 'elavDeg', 'azimDeg', 'trialStartFrames', 'trialEndFrames', 'stimLocs', 'actionLocs');

% Plot stimulation and actions first
% Then plot inter-trial intervals
% Finally plot actual eye movement data
% ELEVATION PLOT
figure; hold on
patch(xTrials, yStim, stimColors, 'EdgeColor', 'none');
patch(xTrials, yAction, actionColors, 'EdgeColor', 'none');
if (~isempty(trialStartFrames))
    patch(xPause, yPause, trialColor, 'EdgeColor', trialColor);
end
h = plot(frameStart:frameStop, elavDeg(:, 1), 'r', frameStart:frameStop, elavDeg(:, 2), 'b');
title([collageFileName(1:end-4) ': Pupil Elevation'], 'Interpreter', 'none');
ylabel('elevation rel. to first frame (deg, + right, - left)');
xlabel('frame #');
for i = 1:length(allColors)
    p(i) = patch(NaN, NaN, allColors(i,:));
end
if (numStim == 4)
    legend([h;p'], 'left eye', 'right eye', 'left near', 'left far', 'right near', 'right far');
elseif (numStim == 3)
    legend([h;p'], 'left eye', 'right eye', 'left', 'right', 'center');    
end
ylim([ymin ymax]);

% AZIMUTH PLOT
figure; hold on
p1 = patch(xTrials, yStim, stimColors, 'EdgeColor', 'none');
p2 = patch(xTrials, yAction, actionColors, 'EdgeColor', 'none');
if (~isempty(trialStartFrames))
    patch(xPause, yPause, trialColor, 'EdgeColor', trialColor);
end
h = plot(frameStart:frameStop, azimDeg(:, 1), 'r', frameStart:frameStop, azimDeg(:, 2), 'b');
title([collageFileName(1:end-4) ': Pupil Azimuth'], 'Interpreter', 'none');
ylabel('azimuth rel. to first frame (deg, + right, - left)');
xlabel('frame #');
for i = 1:length(allColors)
    p(i) = patch(NaN, NaN, allColors(i,:));
end
if (numStim == 4)
    legend([h;p'], 'left eye', 'right eye', 'left near', 'left far', 'right near', 'right far');
elseif (numStim == 3)
    legend([h;p'], 'left eye', 'right eye', 'left', 'right', 'center');    
end
ylim([ymin ymax]);

close(vout);

toc

beep

end