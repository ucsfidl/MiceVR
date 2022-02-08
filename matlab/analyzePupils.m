function analyzePupils(mouseName, day, rig, pxPerMm, useCR, useEyeMeasurements)
% Once trackPupils is done this script is used to analyze the pupil positions and produce many plots.

% It now also keeps track of eye blink times, based on when the centroid is lost.  This is VERY rough and should be 
% checked by hand, as it depends on the quality of the contrast of the eye video.  If contrast is poor, centroid will
% be lost even though there is no eye blink.  If contrast is good, this works pretty well.

% ISSUE: because frames are dropped to determine when a trial starts and
% ends, the overall time plotted will be lagging more and more over time.
% Fix this at some point if it is important.

% Previously (prior to 5/11/2020), Rp was degPerPx, which was an approximation.  
% Now that I can measure Rp for each mouse's eye, the code has been updated to expect Rp, which is the size of the 
% radius from the corneal curvature center to the pupil.  Note that this is the correct value to use for 
% azimuthal deviations, but a different calculation is better done for elevation deviations (see Zoccolan..Cox 2010).
% Along with this correction, we have also added a pxPerMm calibration value, as Rp is in mm.

% By default, average eye movement plots exclude correction and extinction trials

% USAGE:
% >> analyzePupils(['Dragon_229_trk.mat'], 4, [1 0], 1.15, 47, 60, 0)

%leftColor = [1 1 0.79];  % off-yellow
%rightColor = [0.81 1 0.81];  % off-green

% OLD ARGUMENTS
if (day < 10)
    dayStr = ['00' num2str(day)];
elseif (day < 100)
    dayStr = ['0' num2str(day)];
else
    dayStr = num2str(day);
end
trackFileName = [mouseName '_' dayStr '_trk.mat'];

correctNonFlatCurve = 1;  % Used to be an argument, but never changed, so hard-coding here
gRP = [];
usePupilDiamToCalcRp = 1;
analFrameLim = [1 0];

fps = 60;
timeDivider = 60*60; % time scale in minutes
scatterStepSize = 1;  % >= 1; e.g. 100 means 1 in 100 frames are used to plot pupil and CR positions (or 1% sampling).

rigPxPerMm = loadRigPxPerMm();

if (rig > 0 && isempty(pxPerMm))
    pxPerMm = rigPxPerMm(rig, :);
    disp(['USING STORED RIG CALIBRATION = ' num2str(pxPerMm)]);
end

actionsFolder = getActionsFolder();

eyeMeasureFileName = 'UCB_mouse_eye_measurements.m';
run(eyeMeasureFileName);  % Bring those variables into the current workspace

leftEyeColor = 'b'; 
rightEyeColor = 'r';
leftEyeVarianceColor = [0.74 0.87 1];  % light blue
rightEyeVarianceColor = [1 0.77 0.84]; % light red
varianceAlpha = 0.5;

colorLeft = [2 87 194]/255;  % blue
colorRight = [226 50 50]/255; % red
colorCenter = [15 157 88]/255;  % green

colorLeftFar = [2 193 193]/255; % cyan 
colorRightFar = [242 183 124]/255; % orange

shadingColorLeft = [0.84 0.89 0.99];  % dull blue
shadingColorLeftFar = [0.84 0.98 0.99]; % dull cyan
shadingColorRight = [1 0.87 0.71]; % dull orange
shadingColorRightFar = [1 1 0.79];  % dull yellow
shadingColorCenter = [0.85 1 0.8]; % dull green
shadingColorInterTrial = [0.9 0.9 0.9];  % grey
shadingColorCatch = [0.5 0.5 0.5];  % light grey

numWorlds = 1;  % This gets updated if we find more worldnames in the actions file name, and multiple sets of plots get shown

% Used as the full trace plot limits, where this is actually the xmin and xmax for the azimuth plot
ymin = -45;
ymax = 45;

% Used for the average plot limits
avgXMin = -30;
avgXMax = 30;

lw = 1.5; % LineWidth

optoYmin = ymin;  % Where the opto stripe will be drawn
optoYmax = ymin+2;
colorOpto = [0    0    0;
             0.13 0.78 0.78; % left opto = blue [cyan] (Democrats are blue and are left)
             1    0    0; % right opto = red (Republicans are red and are right)
             0.5  0    0.5]; % both opto = purple (blue + red)
optoVarianceAlpha = 0.15;  % 0.1 is too dim, 0.25 is too dark
optoNone = -1;
optoLeft = 0;
optoRight = 1;
optoBoth = 2;

stimCenter = 20000;

% Determine numStim by taking the trackFileName and finding the associated actions file and parsing its filename
if (~contains(trackFileName, '_part_trk.mat'))
    rootFileName = trackFileName(1:end-8);  % Format is [mouseName]_[dayNumWithPreceding zeros]
else
    rootFileName = trackFileName(1:end-13);
end
parts = split(rootFileName, '_');  % Should give 2 parts, mouseName and dayNum
mouseName = parts{1};
dayStr = parts{2};
dayNum = str2double(dayStr);

% Run cleanupTrialTimes here - used to have to run it separately, but just build it in now.
cleanUpTrialTimes([mouseName '_' dayStr '.mat']);

numStim = [];
% Find the actions file in the actions folder.  Remove preceding 0's from dayNum (parts{2}) doing the trick below
actionsFileList = dir([actionsFolder mouseName '-D' num2str(dayNum) '-*actions.txt']);

if (length(actionsFileList) == 1)
    actionsFileName = [actionsFileList(1).folder '\' actionsFileList(1).name];
    parts = split(actionsFileList(1).name, '-');  % e.g. Kalbi-D104-4_BG_Bl_R_30-S1 or Taro-D117-2L_2R_BG_NoCo-S3
    % Find first numbers after the day, and use that as the numStim - there can be up to 2
    levelName = split(parts{3}, '_');
    numStim(1) = str2double(levelName{1}(1));
    % Check and see if there is a second world by seeing if second string begins with a number
    if (isstrprop(levelName{2}(1), 'digit'))
        numStim(2) = str2double(levelName{2}(1));
    end
else
    error('Found 0 or 2+ corresponding actions files.  Should only be 1.');
end

% Might need to do something smarter if there are multiple worlds (e.g. separate plots)
if (numStim(1) == 4 || sum(numStim) == 4)
    legendColors = [shadingColorLeft; shadingColorLeftFar; shadingColorRight; shadingColorRightFar; shadingColorCatch; ...
                    shadingColorInterTrial];
elseif (numStim(1) == 3)
    if (length(numStim) > 1 && numStim(2) == 3)  % This is a multiworld scenario, so keep 2 sets of colors
        numWorlds = length(numStim);
        legendColors = [shadingColorLeft; shadingColorLeftFar; shadingColorRight; shadingColorRightFar; ...
                        shadingColorCenter; shadingColorCatch; shadingColorInterTrial];
    else
        legendColors = [shadingColorLeft; shadingColorRight; shadingColorCenter; shadingColorCatch; shadingColorInterTrial];
    end
elseif (numStim(1) == 1)
    legendColors = [1 1 1];
elseif (numStim(1) == 2)
    if (length(numStim) > 1 && numStim(2) == 2)  % This is a multiworld scenario, so keep 2 sets of colors
        numWorlds = length(numStim);
        legendColors = [shadingColorLeftNear; shadingColorLeftFar; shadingColorRightNear; shadingColorRightFar; ...
                        shadingColorCatch; shadingColorInterTrial];
    else
        legendColors = [shadingColorLeft; shadingColorRight; shadingColorCatch; shadingColorInterTrial];
    end
else
    error("Unsupported number of stim.");
end

frameStart = analFrameLim(1);
frameStop = analFrameLim(2);

trialStartOffset = 1;  % Add this much to the recorded trial frame starts - for backwards compatibility
trialEndOffset = 0;

load(trackFileName, 'vLeftFileName', 'vRightFileName', 'centers', 'areas', 'majorAxisLengths', 'minorAxisLengths', ...
                    'sumImLR', 'frameLim');
% This is to make sure these variables are of the kind that the following code expectes.
% Adi's first version of DLC analysis did not save these as the right kind.
sumImLR = single(sumImLR);
frameLim = double(frameLim);

if (useCR(1) || useCR(2))
    load(trackFileName, 'crCenters', 'crAreas', 'crMajorAxisLengths', 'crMinorAxisLengths');
end

% Open the video files, which will be used to generate a graphic of an average image overlaid with pupil positions
skip = zeros(1,2);
if (~isempty(vLeftFileName))
    v(1) = VideoReader(vLeftFileName); % v(1) is the video of the left eye
    [folder, name, ~] = fileparts(vLeftFileName);
else
    skip(1) = true;
end
if (~isempty(vRightFileName))
    if (~exist('v'))
        v(1) = VideoReader(vRightFileName);
    end
    v(2) = VideoReader(vRightFileName); % v(2) is the video of the right eye
    [folder, name, ~] = fileparts(vRightFileName);
else
    skip(2) = true;
end
if (skip(1) && skip(2))
    error('Need to specify at least 1 of 2 video filenames.');
end
if (isempty(folder))
    folder = '.';
end
outRoot = [folder '\' name(1:end-2)];
% Set a default video if only 1 video was recorded
if (skip(1))
    defVid = 2;
else
    defVid = 1;
end
relFrame = 1;

if (skip(1))
    startVid = 2;
    stopVid = 2;
elseif (skip(2))
    startVid = 1;
    stopVid = 1;
else
    startVid = 1;
    stopVid = 2;
end

totalFrames = length(centers);

% First, quickly calculate the eye blinks which will be stored in the data file
eyeblinkStartFrames = cell(1,2);  % first is left eye eyeblink start times, second is right eye eyeblink start times
for i=1:2
    ebD = diff(isnan(centers(:, 1, i)));
    eyeblinkStartFrames{i} = find(ebD == 1) + 1;
end

if frameStop == 0 || frameStop > totalFrames
    frameStop = totalFrames;
end

% GENERATE average image, to show motion of pupil on top of, if it doesn't already exist.  
% trackPupils run prior to 6/7/2020 will not have it exist.
% trackPupils run after 6/7/2020 will have already generated it.
if (~exist('sumImLR'))
    imLR = zeros(v(defVid).Height, v(defVid).Width, v(defVid).BitsPerPixel/8, 2, 'uint8');
    %minImLR = ones(v(defVid).Height, v(defVid).Width, v(defVid).BitsPerPixel/8, 2, 'uint8')*255;
    sumImLR = zeros(v(defVid).Height, v(defVid).Width, v(defVid).BitsPerPixel/8, 2, 'single');
    frameCnt = 0;
    while frameStart + relFrame <= frameStop + 1
        for i=startVid:stopVid  % 1 is L, 2 is R
            imLR(:,:,:,i) = read(v(i), frameStart + relFrame);
            %minImLR(:,:,:,i) = min(imLR(:,:,:,i), minImLR(:,:,:,i));
            sumImLR(:,:,:,i) = sumImLR(:,:,:,i) + single(imLR(:,:,:,i));
        end

        if (mod(relFrame, 10000) == 1)
            disp(['processed frame ' num2str(relFrame)]);
        end
        relFrame = relFrame + 1000;  % Subsampling 1/1000 gives average image very close to no subsampling, so do this or larger subsample
        frameCnt = frameCnt + 1;
    end
else
    vidFrameStart = frameLim(1);
    vidFrameStop = frameLim(2);
    if vidFrameStop == 0 || vidFrameStop > totalFrames
        vidFrameStop = totalFrames;
    end

    frameCnt = vidFrameStop - vidFrameStart + 1;
end

% Plot all pupil positions over the session.
% This is used to check for incorrect tracking by the software
if (correctNonFlatCurve)
    for i=startVid:stopVid  % 1 is L, 2 is R
        sumImLR(:,:,:,i) = sumImLR(:,:,:,i) ./ frameCnt;
        % Show centers over this average image - commented out for now
        %{
        figure;
        imshow(sumImLR(:,:,:,i)/255, 'InitialMagnification','fit');
        hold on
        if (i == 1)
            title([rootFileName ': Left eye, unaltered'], 'Interpreter', 'none');
        else
            title([rootFileName ': Right eye, unaltered'], 'Interpreter', 'none');
        end
        scatter(centers(1:scatterStepSize:end,1,i), centers(1:scatterStepSize:end,2,i), 4, 'r', 'o', 'filled');
        if (useCR(i))
            scatter(crCenters(1:scatterStepSize:end,1,i), crCenters(1:scatterStepSize:end,2,i), 4, 'b', 'o', 'filled');
        end
        %}
        % Next, fit a line to the pupil centers, and use the angle of that line to the horizontal to rotate
        % all of the centers.  These rotated centers will be used for the subsequent analysis.
        pupX = centers(~isnan(centers(:,1,i)),1,i);
        pupY = centers(~isnan(centers(:,2,i)),2,i);
        [c, S] = polyfit(pupX, pupY, 1);
        m = c(1);
        b = c(2);
        %[y_fit,delta] = polyval(c, pupX, S);
        %figure
        %scatter(centers(:,1,i), centers(:,2,i), 4, 'r', 'o', 'filled');
        %hold on
        %plot(pupX, y_fit, 'k-', 'LineWidth', 2)
        %set(gca, 'YDir','reverse')
        R_sq = 1 - (S.normr/norm(pupY - mean(pupY)))^2;
        rotDeg = atand(m);  % rotation needed to flatten out portion of the ellipse
        disp(['Rotated centers by ' num2str(round(rotDeg, 2)) ' deg']);
        % Subtract the mean before rotating.  This isn't absolutely necessary, but it keeps the CR and pupil centers
        % in the same rough region of the video frame, which makes visualizing slightly more sensible.
        % The exact mean doesn't matter, as long as the same mean is used both for CR and the pupil centers.
        if (useCR(i))
            mx = nanmedian(cat(1, centers(:,1,i), crCenters(:,1,i)));
            my = nanmedian(cat(1, centers(:,2,i), crCenters(:,2,i)));
        else
            mx = nanmedian(centers(:,1,i));
            my = nanmedian(centers(:,2,i));
        end
        %disp(num2str(mx));
        %disp(num2str(my));
        centers(:,1,i) = centers(:,1,i) - mx;
        centers(:,2,i) = centers(:,2,i) - my;
        if (useCR(i))
            crCenters(:,1,i) = crCenters(:,1,i) - mx;
            crCenters(:,2,i) = crCenters(:,2,i) - my;
        end
        rotM = rot2d(-rotDeg); % make the ellipse horizontal by getting its slope to 0
        centers(:,:,i) = transpose(rotM * centers(:,:,i)');  % now the centers have been rotated!
        if (useCR(i))
            crCenters(:,:,i) = transpose(rotM * crCenters(:,:,i)');
        end
        % Add the means back
        centers(:,1,i) = centers(:,1,i) + mx;
        centers(:,2,i) = centers(:,2,i) + my;
        if (useCR(i))
            crCenters(:,1,i) = crCenters(:,1,i) + mx;
            crCenters(:,2,i) = crCenters(:,2,i) + my;
        end
        % Plot just to confirm
        figure;
        imshow(sumImLR(:,:,:,i)/255, 'InitialMagnification','fit');
        hold on
        if (i == 1)
            title([rootFileName ': Left eye, straightened, censored'], 'Interpreter', 'none');
        else
            title([rootFileName ': Right eye, straightened, censored'], 'Interpreter', 'none');
        end
        % Show all centers, so I can see how often it gets it wrong
        % In fact, I can use deviations away from the point cloud as false alarms and censor post-hoc!  TODO.
        % Now, just show points where neither CR nor pupil centers are NaN, as then I can use this image to 
        % check for false positives of CRs and pupil centers.
        if (useCR(i))
            discard = isnan(centers(1:scatterStepSize:end,1,i)) | isnan(crCenters(1:scatterStepSize:end,1,i));
            newCentersX = centers(1:scatterStepSize:end,1,i);
            newCentersY = centers(1:scatterStepSize:end,2,i);
            newCrCentersX = crCenters(1:scatterStepSize:end,1,i);
            newCrCentersY = crCenters(1:scatterStepSize:end,2,i);
            newCentersX = newCentersX(~discard);
            newCentersY = newCentersY(~discard);
            newCrCentersX = newCrCentersX(~discard);
            newCrCentersY = newCrCentersY(~discard);
            
            scatter(newCentersX, newCentersY, 4, 'r', 'o', 'filled');
            scatter(newCrCentersX, newCrCentersY, 4, 'b', 'o', 'filled');
        else
            scatter(centers(1:scatterStepSize:end,1,i), centers(1:scatterStepSize:end,2,i), 4, 'r', 'o', 'filled');
        end
        
    end
end

% Second, record pupil sizes over the duration of the session
% Take the areas matrix and assume each is an area of a circle (approximation). 
% Then take the pxPerMM for each eye and 
% We do 2 measures - a simplistic one assuming we are looking at a non-deformed circle
areasMm2 = cat(3, areas(:,:,1) * (1/pxPerMm(1)^2), areas(:,:,2) * (1/pxPerMm(2)^2));
% and a second one which assumes the longest ellipse axis is the diameter (due to foreshortening of the minor axis)
majorAxisMm = cat(3, majorAxisLengths(:,:,1) / pxPerMm(1), majorAxisLengths(:,:,2) / pxPerMm(2));

% Instead of using a static Rp, use one based on pupil size.  So Rp varies during the entire task.
% First, lookup the slope and intercept in the data file:
if (useEyeMeasurements && isKey(mouseToRpLine, mouseName))
    rpline = mouseToRpLine(mouseName);
    slope = [rpline(1,1) rpline(2,1)];
    yint = [rpline(1,2) rpline(2,2)];
    disp(['slope = ' num2str(slope)]);
    disp(['yintercept = ' num2str(yint)]);
else  % If nothing found, use the values from Stahl 2002
    slope = [-0.142 -0.142];
    yint = [0.925 0.925];  % old defaults from paper: [1.055 1.055]
    disp('Eye measurements NOT FOUND, so using default values');
end
Rp(:,:,1) = slope(1)*majorAxisMm(:,:,1) + yint(1);
Rp(:,:,2) = slope(2)*majorAxisMm(:,:,2) + yint(2);

if (usePupilDiamToCalcRp)
    RpL = Rp(:,:,1);
    RpR = Rp(:,:,2);
else
    RpL = gRp(1);
    RpR = gRp(2);
end

% Process the position changes for plotting later.
% Either, find the central position of the eye, given all of the data, and subtract that away.
% Or, if an on-axis corneal reflection is present, use that as a landmark, calculate the center relative to that, and subtract away.
elavCenter = nanmean(centers(:, 2, :));
elavDeg = cat(3, real(asind(((elavCenter(:,:,1) - centers(:,2,1))/pxPerMm(1)) ./ RpL)), ...
                 real(asind(((elavCenter(:,:,2) - centers(:,2,2))/pxPerMm(2)) ./ RpR)));
elavDeg = reshape(elavDeg, size(elavDeg, 1), size(elavDeg, 3));

if (useCR(1) || useCR(2))
    if (useCR(1))
        cL = crCenters(:,1,1);
    else
        cL = nanmean(centers(:, 1, 1));
    end
    if (useCR(2))
        cR = crCenters(:,1,2);
    else
        cR = nanmean(centers(:, 1, 2));
    end
    
    azimDeg = cat(3, real(asind((cL - centers(:,1,1))/pxPerMm(1)) ./ RpL), ...
                     real(asind((cR - centers(:,1,2))/pxPerMm(2)) ./ RpR));
    azimCenter = nanmean(azimDeg);
    azimDeg = cat(3, azimDeg(:,1,1) - azimCenter(:,:,1), azimDeg(:,1,2) - azimCenter(:,:,2));
else
    azimCenter = nanmean(centers(:, 1, :));
    azimDeg = cat(3, real(asind(((azimCenter(:,:,1) - centers(:,1,1))/pxPerMm(1)) ./ RpL)), ...
                     real(asind(((azimCenter(:,:,2) - centers(:,1,2))/pxPerMm(2)) ./ RpR)));
end
azimDeg = reshape(azimDeg, size(azimDeg, 1), size(azimDeg, 3));

% Read the actions file so the graph can be properly annotated
stimIdxs = [];
actionIdxs = [];
optoStates = [];
worldIdxs = [];  % Need this for shading the elevation and azimuth plots properly

actionsFile = fopen(actionsFileName);
if (actionsFile ~= -1) % File found
    fgetl(actionsFile);  % First line is a header so ignore
    trialRecs = textscan(actionsFile, getActionLineFormat()); 
    for i=1:length(trialRecs{1})
        stimIdx = getStimIdx(trialRecs, i);
        stimIdxs = [stimIdxs stimIdx];

        actIdx = getActionIdx(trialRecs, i);
        actionIdxs = [actionIdxs actIdx];

        optoState = getOptoLoc(trialRecs, i);
        optoStates = [optoStates optoState];
        
        worldIdx = getWorldIdx(trialRecs, i);
        worldIdxs = [worldIdxs worldIdx];
    end
else
    error('No actions file found.  Be sure the name matches the track file name.');
end

% If trial times are available, incorporate into the graphs
load([rootFileName '_corr.mat'], 'trialStarts', 'trialEnds');
if (trialStarts(1).FrameNumber ~= 0)  % new format includes 0 as first index, but old format didn't, so permit backwards compatibility
    trialStartFrames = [1-trialStartOffset trialStarts.FrameNumber] + trialStartOffset; % First trial start is not written
else
    trialStartFrames = [1 trialStarts(2:end).FrameNumber]; % First trial start is not written
end

if (exist('trialEnds', 'var'))
    if (length(trialStarts) == length(trialEnds))
        trialEndFrames = [trialEnds.FrameNumber] + trialEndOffset; % Last trial end was written
    else
        trialEndFrames = [trialEnds.FrameNumber totalFrames-trialEndOffset] + trialEndOffset; % Last trial end was not written, because game was interrupted
    end
else
    trialEndFrames = [trialStartFrames(2:end) totalFrames];
end

numCompletedTrials = length(stimIdxs);

% If game ended mid-trial, don't color the final trial
if (length(trialStartFrames) > numCompletedTrials)
    trialStartFrames = trialStartFrames(1:end-1);
    trialEndFrames = trialEndFrames(1:end-1);
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
if (trimmedEnds(end) < frameStop && frameStop ~= totalFrames && length(trimmedStarts) == length(trimmedEnds) + 1)
    trimmedEnds = [trimmedEnds frameStop];
end
xTrials = cat(1, trimmedStarts, trimmedEnds);
xTrials = cat(1, xTrials, flipud(xTrials));

yStim = [ymax; ymax; 0; 0];
yStim = repmat(yStim, 1, size(xTrials,2));
yAction = [0; 0; ymin; ymin];
yAction = repmat(yAction, 1, size(xTrials,2));
yOpto = [optoYmin; optoYmin; optoYmax; optoYmax];
yOpto = repmat(yOpto, 1, size(xTrials,2));

xPause = cat(1, trimmedEnds(1:end-1), trimmedStarts(2:end));
xPause = cat(1, xPause, flipud(xPause));
yPause = [ymax; ymax; ymin; ymin];
yPause = repmat(yPause, 1, size(xPause,2));

stimPatchColors = zeros(1, numCompletedTrials, 3);  % Stim location colors to to use when plotting
actionPatchColors = zeros(1, numCompletedTrials, 3);
optoColors = zeros(1, numCompletedTrials, 3);

% Determine colors to use to shade the elevation and azimuth plots.
% Even if there are multiple worlds, this shading should be in a single color array that is the length of the number of trials
for i=1:numCompletedTrials
    currWorldIdx = worldIdxs(i) + 1;
    currWorldNumStim = numStim(currWorldIdx);
    if (currWorldNumStim == 4)
        if (stimIdxs(i) == 0)
            stimPatchColors(1,i,:) = shadingColorLeft;
        elseif (stimIdxs(i) == 1)
            stimPatchColors(1,i,:) = shadingColorRight;
        elseif (stimIdxs(i) == 2)
            stimPatchColors(1,i,:) = shadingColorLeftFar;
        elseif (stimIdxs(i) == 3)
            stimPatchColors(1,i,:) = shadingColorRightFar;
        elseif (stimIdxs(i) == -1)
            stimPatchColors(1,i,:) = shadingColorCatch;
        end
        
        if (actionIdxs(i) == 0)
            actionPatchColors(1,i,:) = shadingColorLeft;
        elseif (actionIdxs(i) == 1)
            actionPatchColors(1,i,:) = shadingColorRight;
        elseif (actionIdxs(i) == 2)
            actionPatchColors(1,i,:) = shadingColorLeftFar;
        elseif (actionIdxs(i) == 3)
            actionPatchColors(1,i,:) = shadingColorRightFar;
        end
        
        optoColors(1,i,:) = colorOpto(optoStates(i) + 2,:);
    elseif (currWorldNumStim == 2 || currWorldNumStim == 3)
        if (length(levelName{currWorldIdx}) == 1)  % It is the normal 2-choice or 3-choice level
            if (stimIdxs(i) == 0)
                stimPatchColors(1,i,:) = shadingColorLeft;
            elseif (stimIdxs(i) == 1)
                stimPatchColors(1,i,:) = shadingColorRight;
            elseif (stimIdxs(i) == 2)
                stimPatchColors(1,i,:) = shadingColorCenter;
            elseif (stimIdxs(i) == -1)
                stimPatchColors(1,i,:) = shadingColorCatch;
            end
            
            if (actionIdxs(i) == 0)
                actionPatchColors(1,i,:) = shadingColorLeft;
            elseif (actionIdxs(i) == 1)
                actionPatchColors(1,i,:) = shadingColorRight;
            else
                actionPatchColors(1,i,:) = shadingColorCenter;
            end
        elseif (levelName{currWorldIdx}(2) == 'L')
            if (stimIdxs(i) == 0)
                stimPatchColors(1,i,:) = shadingColorLeft;
            elseif (stimIdxs(i) == 1)
                stimPatchColors(1,i,:) = shadingColorLeftFar;
            elseif (stimIdxs(i) == 2)
                stimPatchColors(1,i,:) = shadingColorCenter;
            elseif (stimIdxs(i) == -1)
                stimPatchColors(1,i,:) = shadingColorCatch;
            end
            
            if (actionIdxs(i) == 0)
                actionPatchColors(1,i,:) = shadingColorLeft;
            elseif (actionIdxs(i) == 1)
                actionPatchColors(1,i,:) = shadingColorLeftFar;
            else
                actionPatchColors(1,i,:) = shadingColorCenter;
            end
        elseif (levelName{currWorldIdx}(2) == 'R')
            if (stimIdxs(i) == 0)
                stimPatchColors(1,i,:) = shadingColorRight;
            elseif (stimIdxs(i) == 1)
                stimPatchColors(1,i,:) = shadingColorRightFar;
            elseif (stimIdxs(i) == 2)
                stimPatchColors(1,i,:) = shadingColorCenter;
            elseif (stimIdxs(i) == -1)
                stimPatchColors(1,i,:) = shadingColorCatch;
            end

            if (actionIdxs(i) == 0)
                actionPatchColors(1,i,:) = shadingColorRight;
            elseif (actionIdxs(i) == 1)
                actionPatchColors(1,i,:) = shadingColorRightFar;
            else
                actionPatchColors(1,i,:) = shadingColorCenter;
            end
        else
            error('unexpected 2-choice or 3-choice level based on levelName!');
        end
        
        optoColors(1,i,:) = colorOpto(optoStates(i) + 2,:);
    elseif (currWorldNumStim == 1)
        stimPatchColors(1,i,:) = [1 1 1];
        actionPatchColors(1,i,:) = [1 1 1];
        optoColors(1,i,:) = colorOpto(optoStates(i) + 2,:);
    end    
end

stimPatchColors = stimPatchColors(1, 1+numTruncatedStart:end-numTruncatedEnd, :);
actionPatchColors = actionPatchColors(1, 1+numTruncatedStart:end-numTruncatedEnd, :);

% Collect data for averaging...
totalNumStim = sum(numStim);
trimmedStimIdxs = stimIdxs(1+numTruncatedStart:end-numTruncatedEnd);
trimmedActionIdxs = actionIdxs(1+numTruncatedStart:end-numTruncatedEnd);
trimmedWorldIdxs = worldIdxs(1+numTruncatedStart:end-numTruncatedEnd);
stimEyeMoveTrials = cell(totalNumStim, 2);  % 2, one for each eye
stimEyeMoveTrialsCatch = cell(1, 2);
actionEyeMoveTrials = cell(totalNumStim, 2); % 2, one for each eye
stimActionEyeMoveTrials = cell(totalNumStim, totalNumStim, 2);  % first axis is stimIdx, second is actionIdx, third is left/right eye
stimOptoEyeMoveTrials = cell(totalNumStim, 4, 2); % first axis is stimLoc, second is optoState (OFF, LEFT, RIGHT, or BOTH), third is left/right eye
for i=1:length(trimmedStimIdxs)
    if (trimmedWorldIdxs(i) == 0)
        stimNum = trimmedStimIdxs(i) + 1;
        actionNum = trimmedActionIdxs(i) + 1;
    elseif (trimmedWorldIdxs(i) == 1)
        stimNum = numStim(1) + trimmedStimIdxs(i) + 1;
        actionNum = numStim(1) + trimmedActionIdxs(i) + 1;
    else
        error('only supports levels with a maximum of 2 worlds - edit code to support more.');
    end
    
    if (stimNum == 0) % catch trial
        for j=1:2
            s = stimEyeMoveTrialsCatch{1, j};
            s{end+1} = azimDeg(trimmedStarts(i):trimmedEnds(i)-1, j);
            stimEyeMoveTrialsCatch{1, j} = s;
        end
        continue;
    end
    
    % Added 10/25/20 - Filter out extinction and correction trials from average graphs
    if (getExtinction(trialRecs, i) || getCorrection(trialRecs, i))
        continue;
    end
    
    for j=1:2
        s = stimEyeMoveTrials{stimNum, j};
        s{end+1} = azimDeg(trimmedStarts(i):trimmedEnds(i)-1, j);
        stimEyeMoveTrials{stimNum, j} = s;

        a = actionEyeMoveTrials{actionNum, j};
        a{end+1} = azimDeg(trimmedStarts(i):trimmedEnds(i)-1, j);
        actionEyeMoveTrials{actionNum, j} = a;

        sa = stimActionEyeMoveTrials{stimNum, actionNum, j};
        sa{end+1} = azimDeg(trimmedStarts(i):trimmedEnds(i)-1, j);
        stimActionEyeMoveTrials{stimNum, actionNum, j} = sa;

        osIdx = optoStates(i) + 2;  % Shift from the read value {-1,2) to the index (1,4)
        so = stimOptoEyeMoveTrials{stimNum, osIdx, j};
        so{end+1} = azimDeg(trimmedStarts(i):trimmedEnds(i)-1, j);
        stimOptoEyeMoveTrials{stimNum, osIdx, j} = so;
    end
end

% First, plot the stimulus average of the eye movements for all trials (incorrect and correct)
% Normalize by resampling, and then plot the average of the resampled eye movements.
minLengths = zeros(2, 1);  % One for each eye
resizedStimEye = cell(size(stimEyeMoveTrials));
m = cell(size(stimEyeMoveTrials));
sem = cell(size(stimEyeMoveTrials));
ySem = cell(size(stimEyeMoveTrials));
for eye=1:2  % For each eye
    h = [];
    n = 0;
    stimEyeLengths = [];
    for stimIdx=1:totalNumStim
        if (~isempty(stimEyeMoveTrials{stimIdx, eye})) % Prevents error if there are no stim of a type in a session
            stimEyeLengths = [stimEyeLengths cellfun(@(x) length(x), stimEyeMoveTrials{stimIdx,eye})];
            n = n + length(stimEyeMoveTrials{stimIdx,eye});
        end
    end
    minLengths(eye) = min(stimEyeLengths);  % Use min instead of max, as max adds some artifacts at the end, and both give the same shape
    
    for stimIdx=1:totalNumStim
        if (~isempty(stimEyeMoveTrials{stimIdx,eye}))
            resizedStimEye{stimIdx,eye} = cellfun(@(x) interp1(1:length(x), x, 1:length(x)/minLengths(eye):length(x))',...
                                                    stimEyeMoveTrials{stimIdx,eye}, 'UniformOutput', false);
            d = cell2mat(resizedStimEye{stimIdx,eye}(:)');
            m{stimIdx,eye} = nanmean(d, 2);
            sem{stimIdx,eye} = nanstd(d, [], 2) ./ sqrt(size(d, 1));
        end
    end

    for stimIdx=1:totalNumStim
        ySem{stimIdx,eye} = [m{stimIdx,eye}'-sem{stimIdx,eye}', fliplr(m{stimIdx,eye}'+sem{stimIdx,eye}')];
    end
    
    figure; hold on;
    x = 1:length(m{1,eye}); % All the lengths are the same for each stim for 1 eye, so just pull from 1
    plot(zeros(1,length(m{1,eye})), x, 'k--');  % First plot dashed 0 line to indicate the center
    xSem = cat(2, x, fliplr(x));
    for stimIdx=1:totalNumStim
        adjStimIdx = stimIdx;
        worldIdx = 1;
        if (stimIdx > numStim(1))
            adjStimIdx = stimIdx - numStim(1);
            worldIdx = 2;
        end
        
        if (numStim(worldIdx) == 2 || numStim(worldIdx) == 3)
            if (length(levelName{worldIdx}) == 1)  % Regular 2-choice or 3-choice level
                if (adjStimIdx == 1)
                    curColor = colorLeft;
                    curShadingColor = shadingColorLeft;
                    sLoc = 'Left';
                elseif (adjStimIdx == 2)
                    curColor = colorRight;
                    curShadingColor = shadingColorRight;
                    sLoc = 'Right';
                elseif (adjStimIdx == 3)
                    curColor = colorCenter;
                    curShadingColor = shadingColorCenter;
                    sLoc = 'Center';
                end
            elseif (levelName{worldIdx}(2) == 'L')
                if (adjStimIdx == 1)
                    curColor = colorLeft;
                    curShadingColor = shadingColorLeft;
                    sLoc = 'Left Near';
                elseif (adjStimIdx == 2)
                    curColor = colorLeftFar;
                    curShadingColor = shadingColorLeftFar;
                    sLoc = 'Left Far';
                elseif (adjStimIdx == 3)
                    curColor = colorCenter;
                    curShadingColor = shadingColorCenter;
                    sLoc = 'Center';
                end
            elseif (levelName{worldIdx}(2) == 'R')
                if (adjStimIdx == 1)
                    curColor = colorRight;
                    curShadingColor = shadingColorRight;
                    sLoc = 'Right Near';
                elseif (adjStimIdx == 2)
                    curColor = colorRightFar;
                    curShadingColor = shadingColorRightFar;
                    sLoc = 'Right Far';
                elseif (adjStimIdx == 3)
                    curColor = colorCenter;
                    curShadingColor = shadingColorCenter;
                    sLoc = 'Center';
                end
            end
        elseif (numStim(1) == 4)
            if (adjStimIdx == 1)
                curColor = colorLeft;
                curShadingColor = shadingColorLeft;
                sLoc = 'Left Near';
            elseif (adjStimIdx == 2)
                curColor = colorRight;
                curShadingColor = shadingColorRight;
                sLoc = 'Right Near';
            elseif (adjStimIdx == 3)
                curColor = colorLeftFar;
                curShadingColor = shadingColorLeftFar;
                sLoc = 'Left Far';
            elseif (adjStimIdx == 4)
                curColor = colorRightFar;
                curShadingColor = shadingColorRightFar;
                sLoc = 'Right Far';
            end
        elseif (numStim(1) == 1)
            curColor = [0 0 0];
            curShadingColor = [0.5 0.5 0.5];
            sLoc = 'Straight';
        end
        
        if (~isempty(ySem{stimIdx, eye}))
            patch(ySem{stimIdx,eye}, xSem, curShadingColor, 'EdgeColor', 'none');
            alpha(varianceAlpha);
            h = [h plot(m{stimIdx,eye}, x, 'Color', curColor, 'LineWidth', lw)];
        end
    end
    if (eye == 1)
        eyeName = 'Left';
    else
        eyeName = 'Right';
    end

    title([rootFileName ': ' eyeName ' eye, Correct & Incorrect Trials'], 'Interpreter', 'none');
    annotation('textbox', [.8 0 .2 .2], 'String', ['n=' num2str(n)], 'FitBoxToText', 'on', 'EdgeColor', 'white');  
    if (length(numStim) == 1)
        if (numStim(1) == 3)
            legend(h, 'left stim', 'right stim', 'center stim');
        elseif (numStim(1) == 4)
            legend(h, 'left near stim', 'right near stim', 'left far stim', 'right far stim');
        elseif (numStim(1) == 1)
            legend(h, 'straight');
        elseif (numStim(1) == 2)
            legend(h, 'left stim', 'right stim');
        end
    elseif (length(numStim) == 2)
        if (numStim(1) == 3)
            if (numStim(2) == 3)
                legend(h, 'near left stim', 'far left stim', 'center stim', 'near right stim', 'far right stim', 'center stim');
            end
        elseif (numStim(1) == 2)
            if (numStim(2) == 2)
                legend(h, 'near left stim', 'far left stim', 'near right stim', 'far right stim');
            end
        end
    else
        error('too many stim');
    end
    xlim([avgXMin avgXMax]);
    ylim([0 length(x)]);
    yl = ylim;
    yticks(yl(1):60:yl(2));  % Convert to seconds
    yt = yticks;
    ytl = {};
    for j=1:length(yticks)
        ytl{length(ytl)+1} = num2str(yt(j)/60);
    end
    yticklabels(ytl');
    ylabel('time (normalized sec)')
    xlabel('gaze azimuth (deg)');
end


% Second, plot the stimulus average of the eye movements for only CORRECT trials
minLengths = zeros(2, 1);  % One for each eye
resizedStimEye = cell(size(stimEyeMoveTrials));
mu = cell(size(stimEyeMoveTrials));
sem = cell(size(stimEyeMoveTrials));
ySem = cell(size(stimEyeMoveTrials));
for eye=1:2  % For each eye
    h = [];
    n = 0;
    stimCorrectEyeLengths = [];
    % First collect all the trial lengths for the correct trials, to assist with normalizing trial duration
    for stimIdx=1:totalNumStim
        % Account for some sessions where the mouse always gets a specific stim location wrong
        if (~isempty(stimActionEyeMoveTrials{stimIdx,stimIdx,eye}))
            stimCorrectEyeLengths = [stimCorrectEyeLengths cellfun(@(x) length(x), stimActionEyeMoveTrials{stimIdx,stimIdx,eye})];
            n = n + length(stimActionEyeMoveTrials{stimIdx,stimIdx,eye});
        end
    end

    % Next, resample the longer trials to the length of the shortest trial, because all 4 trial types will be plotted on the same axis
    minLengths(eye) = min(stimCorrectEyeLengths);  % Use min instead of max, as max adds some artifacts at the end, and both give the same shape
    for stimIdx=1:totalNumStim
        if (~isempty(stimActionEyeMoveTrials{stimIdx,stimIdx,eye}))
            resizedStimEye{stimIdx,eye} = cellfun(@(x) resample(x, minLengths(eye), length(x)), stimActionEyeMoveTrials{stimIdx,stimIdx,eye}, 'UniformOutput', false);
            d = cell2mat(resizedStimEye{stimIdx,eye}(:)');
            mu{stimIdx,eye} = nanmean(d, 2);
            sem{stimIdx,eye} = nanstd(d, [], 2) ./ sqrt(size(d, 1));
        end
    end
    
    for stimIdx=1:totalNumStim
        if (~isempty(mu{stimIdx,eye}))
            ySem{stimIdx,eye} = [mu{stimIdx,eye}'-sem{stimIdx,eye}', fliplr(mu{stimIdx,eye}'+sem{stimIdx,eye}')];
        end
    end

    figure; hold on;
    skipLegend = zeros(1,totalNumStim);
    for stimIdx=1:totalNumStim
        if (~isempty(mu{stimIdx,eye}))
            x = 1:length(mu{stimIdx,eye});
            plot(zeros(1,length(mu{stimIdx,eye})), x, 'k--');  % This is replotted each time - no biggie
            xSem = cat(2, x, fliplr(x));

            adjStimIdx = stimIdx;
            worldIdx = 1;
            if (stimIdx > numStim(1))
                adjStimIdx = stimIdx - numStim(1);
                worldIdx = 2;
            end

            if (numStim(worldIdx) == 2 || numStim(worldIdx) == 3)
                if (length(levelName{worldIdx}) == 1)  % Regular 2-choice or 3-choice level
                    if (adjStimIdx == 1)
                        curColor = colorLeft;
                        curShadingColor = shadingColorLeft;
                        sLoc = 'Left';
                    elseif (adjStimIdx == 2)
                        curColor = colorRight;
                        curShadingColor = shadingColorRight;
                        sLoc = 'Right';
                    elseif (adjStimIdx == 3)
                        curColor = colorCenter;
                        curShadingColor = shadingColorCenter;
                        sLoc = 'Center';
                    end
                elseif (levelName{worldIdx}(2) == 'L')
                    if (adjStimIdx == 1)
                        curColor = colorLeft;
                        curShadingColor = shadingColorLeft;
                        sLoc = 'Left Near';
                    elseif (adjStimIdx == 2)
                        curColor = colorLeftFar;
                        curShadingColor = shadingColorLeftFar;
                        sLoc = 'Left Far';
                    elseif (adjStimIdx == 3)
                        curColor = colorCenter;
                        curShadingColor = shadingColorCenter;
                        sLoc = 'Center';
                    end
                elseif (levelName{worldIdx}(2) == 'R')
                    if (adjStimIdx == 1)
                        curColor = colorRight;
                        curShadingColor = shadingColorRight;
                        sLoc = 'Right Near';
                    elseif (adjStimIdx == 2)
                        curColor = colorRightFar;
                        curShadingColor = shadingColorRightFar;
                        sLoc = 'Right Far';
                    elseif (adjStimIdx == 3)
                        curColor = colorCenter;
                        curShadingColor = shadingColorCenter;
                        sLoc = 'Center';
                    end
                end
            elseif (numStim(1) == 4)
                if (adjStimIdx == 1)
                    curColor = colorLeft;
                    curShadingColor = shadingColorLeft;
                    sLoc = 'Left Near';
                elseif (adjStimIdx == 2)
                    curColor = colorRight;
                    curShadingColor = shadingColorRight;
                    sLoc = 'Right Near';
                elseif (adjStimIdx == 3)
                    curColor = colorLeftFar;
                    curShadingColor = shadingColorLeftFar;
                    sLoc = 'Left Far';
                elseif (adjStimIdx == 4)
                    curColor = colorRightFar;
                    curShadingColor = shadingColorRightFar;
                    sLoc = 'Right Far';
                end
            elseif (numStim(1) == 1)
                curColor = [0 0 0];
                curShadingColor = [0.5 0.5 0.5];
                sLoc = 'Straight';
            end
        
            patch(ySem{stimIdx,eye}, xSem, curShadingColor, 'EdgeColor', 'none');
            alpha(varianceAlpha);
            h = [h plot(mu{stimIdx,eye}, x, 'Color', curColor, 'LineWidth', lw)];
        else
            skipLegend(stimIdx) = 1;
        end
    end
    if (eye == 1)
        eyeName = 'Left';
    else
        eyeName = 'Right';
    end

    title([rootFileName ': ' eyeName ' eye, CORRECT trials only'], 'Interpreter', 'none');
    annotation('textbox', [.8 0 .2 .2], 'String', ['n=' num2str(n)], 'FitBoxToText', 'on', 'EdgeColor', 'white');  
    if (length(numStim) == 1)
        if (numStim(1) == 3)
            standardLegend = {'left stim', 'right stim', 'center stim'};
        elseif (numStim(1) == 4)
            standardLegend = {'left near stim', 'right near stim', 'left far stim', 'right far stim'};
        elseif (numStim(1) == 1)
            standardLegend = {'straight'};
        elseif (numStim(1) == 2)
            standardLegend = {'left stim', 'right stim'};
        end
    elseif (length(numStim) == 2)
        if (numStim(1) == 3)
            if (numStim(2) == 3)
                standardLegend = {'near left stim', 'far left stim', 'center stim', 'near right stim', 'far right stim', 'center stim'};
            end
        elseif (numStim(1) == 2)
            if (numStim(2) == 2)
                standardLegend = {'near left stim', 'far left stim', 'near right stim', 'far right stim'};
            end
        end
    else
        error('too many stim');
    end
    currLeg = {};
    for i=1:length(standardLegend)
        if (~skipLegend(i))
            currLeg{end+1} = standardLegend{i};
        end
    end
    legend(h, currLeg);

    xlim([avgXMin avgXMax]);
    ylim([0 length(x)]);
    yl = ylim;
    yticks(yl(1):60:yl(2));  % Convert to seconds
    yt = yticks;
    ytl = {};
    for j=1:length(yticks)
        ytl{length(ytl)+1} = num2str(yt(j)/60);
    end
    yticklabels(ytl');
    ylabel('time (normalized sec)')
    xlabel('gaze azimuth (deg)');
end

% COMMENTED OUT TO REDUCE NUMBER OF FIGURES DISPLAYED
% Third, plot the stimulus average of the eye movements only for incorrect trials
% Suppress warnings related to extra legend entries
warning('off','MATLAB:legend:IgnoringExtraEntries')
minLengths = zeros(2, 1);  % One for each eye
resizedStimEye = cell(size(stimEyeMoveTrials));
mu = cell(size(stimEyeMoveTrials));
sem = cell(size(stimEyeMoveTrials));
ySem = cell(size(stimEyeMoveTrials));
for eye=1:2  % For each eye
    h = [];
    n = 0;
    stimIncorrectEyeLengths = [];
    % First collect all the trial lengths for the correct trials, to assist with normalizing trial duration
    for stimIdx=1:totalNumStim
        for actIdx=1:totalNumStim
            if (stimIdx ~= actIdx && ~isempty(stimActionEyeMoveTrials{stimIdx,actIdx,eye}))
                stimIncorrectEyeLengths = [stimIncorrectEyeLengths cellfun(@(x) length(x), stimActionEyeMoveTrials{stimIdx,actIdx,eye})];
                n = n + length(stimActionEyeMoveTrials{stimIdx,actIdx,eye});
            end
        end
    end
    
    % If mouse was perfect with no incorrect trials, exit and move on
    if (n == 0)
        break;
    end

    % Next, resample the longer trials to the length of the shortest trial, because all 4 trial types will be plotted on the same axis
    minLengths(eye) = min(stimIncorrectEyeLengths);  % Use min instead of max, as max adds some artifacts at the end, and both give the same shape
    for stimIdx=1:totalNumStim
        for actIdx=1:totalNumStim
           if (stimIdx ~= actIdx && ~isempty(stimActionEyeMoveTrials{stimIdx,actIdx,eye}))
               cTmp = cellfun(@(x) resample(x, minLengths(eye), length(x)), stimActionEyeMoveTrials{stimIdx,actIdx,eye}, 'UniformOutput', false);
               resizedStimEye{stimIdx,eye} = cat(2,resizedStimEye{stimIdx,eye}, cTmp);
           end
        end
        if (~isempty(resizedStimEye{stimIdx,eye}))
            d = cell2mat(resizedStimEye{stimIdx,eye}(:)');
            mu{stimIdx,eye} = nanmean(d, 2);
            sem{stimIdx,eye} = nanstd(d, [], 2) ./ sqrt(size(d, 1));
        end
    end
    
    for stimIdx=1:totalNumStim
        ySem{stimIdx,eye} = [mu{stimIdx,eye}'-sem{stimIdx,eye}', fliplr(mu{stimIdx,eye}'+sem{stimIdx,eye}')];
    end

    figure; hold on;
    skipLegend = zeros(1,totalNumStim);
    % All the lengths are NOT the same for each stim for 1 eye (some are empty), so pull from 1 that has data
    for stimIdx=1:totalNumStim
        if (~isempty(mu{stimIdx,eye}))
            x = 1:length(mu{stimIdx,eye}); 
            plot(zeros(1,length(mu{stimIdx,eye})), x, 'k--');
            xSem = cat(2, x, fliplr(x));
            break;
        end
    end
    for stimIdx=1:totalNumStim
        if (~isempty(mu{stimIdx,eye}))
            adjStimIdx = stimIdx;
            worldIdx = 1;
            if (stimIdx > numStim(1))
                adjStimIdx = stimIdx - numStim(1);
                worldIdx = 2;
            end

            if (numStim(worldIdx) == 2 || numStim(worldIdx) == 3)
                if (length(levelName{worldIdx}) == 1)  % Regular 2-choice or 3-choice level
                    if (adjStimIdx == 1)
                        curColor = colorLeft;
                        curShadingColor = shadingColorLeft;
                        sLoc = 'Left';
                    elseif (adjStimIdx == 2)
                        curColor = colorRight;
                        curShadingColor = shadingColorRight;
                        sLoc = 'Right';
                    elseif (adjStimIdx == 3)
                        curColor = colorCenter;
                        curShadingColor = shadingColorCenter;
                        sLoc = 'Center';
                    end
                elseif (levelName{worldIdx}(2) == 'L')
                    if (adjStimIdx == 1)
                        curColor = colorLeft;
                        curShadingColor = shadingColorLeft;
                        sLoc = 'Left Near';
                    elseif (adjStimIdx == 2)
                        curColor = colorLeftFar;
                        curShadingColor = shadingColorLeftFar;
                        sLoc = 'Left Far';
                    elseif (adjStimIdx == 3)
                        curColor = colorCenter;
                        curShadingColor = shadingColorCenter;
                        sLoc = 'Center';
                    end
                elseif (levelName{worldIdx}(2) == 'R')
                    if (adjStimIdx == 1)
                        curColor = colorRight;
                        curShadingColor = shadingColorRight;
                        sLoc = 'Right Near';
                    elseif (adjStimIdx == 2)
                        curColor = colorRightFar;
                        curShadingColor = shadingColorRightFar;
                        sLoc = 'Right Far';
                    elseif (adjStimIdx == 3)
                        curColor = colorCenter;
                        curShadingColor = shadingColorCenter;
                        sLoc = 'Center';
                    end
                end
            elseif (numStim(1) == 4)
                if (adjStimIdx == 1)
                    curColor = colorLeft;
                    curShadingColor = shadingColorLeft;
                    sLoc = 'Left Near';
                elseif (adjStimIdx == 2)
                    curColor = colorRight;
                    curShadingColor = shadingColorRight;
                    sLoc = 'Right Near';
                elseif (adjStimIdx == 3)
                    curColor = colorLeftFar;
                    curShadingColor = shadingColorLeftFar;
                    sLoc = 'Left Far';
                elseif (adjStimIdx == 4)
                    curColor = colorRightFar;
                    curShadingColor = shadingColorRightFar;
                    sLoc = 'Right Far';
                end
            elseif (numStim(1) == 1)
                curColor = [0 0 0];
                curShadingColor = [0.5 0.5 0.5];
                sLoc = 'Straight';
            end
            
            patch(ySem{stimIdx,eye}, xSem, curShadingColor, 'EdgeColor', 'none');
            alpha(varianceAlpha);
            h = [h plot(mu{stimIdx,eye}, x, 'Color', curColor, 'LineWidth', lw)];
        else
            skipLegend(stimIdx) = 1;
        end
    end
    
    if (eye == 1)
        eyeName = 'Left';
    else
        eyeName = 'Right';
    end

    title([rootFileName ': ' eyeName ' eye, INCORRECT trials only'], 'Interpreter', 'none');
    annotation('textbox', [.8 0 .2 .2], 'String', ['n=' num2str(n)], 'FitBoxToText', 'on', 'EdgeColor', 'white');  
    if (length(numStim) == 1)
        if (numStim(1) == 3)
            standardLegend = {'left stim', 'right stim', 'center stim'};
        elseif (numStim(1) == 4)
            standardLegend = {'left near stim', 'right near stim', 'left far stim', 'right far stim'};
        elseif (numStim(1) == 1)
            standardLegend = {'straight'};
        elseif (numStim(1) == 2)
            standardLegend = {'left stim', 'right stim'};
        end
    elseif (length(numStim) == 2)
        if (numStim(1) == 3)
            if (numStim(2) == 3)
                standardLegend = {'near left stim', 'far left stim', 'center stim', 'near right stim', 'far right stim', 'center stim'};
            end
        elseif (numStim(1) == 2)
            if (numStim(2) == 2)
                standardLegend = {'near left stim', 'far left stim', 'near right stim', 'far right stim'};
            end
        end
    else
        error('too many stim');
    end
    currLeg = {};
    for i=1:length(standardLegend)
        if (~skipLegend(i))
            currLeg{end+1} = standardLegend{i};
        end
    end
    legend(h, currLeg);

    xlim([avgXMin avgXMax]);
    ylim([0 length(x)]);
    yl = ylim;
    yticks(yl(1):60:yl(2));  % Convert to seconds
    yt = yticks;
    ytl = {};
    for j=1:length(yticks)
        ytl{length(ytl)+1} = num2str(yt(j)/60);
    end
    yticklabels(ytl');
    ylabel('time (normalized sec)')
    xlabel('gaze azimuth (deg)');
end

%{
% Fourth, plot the stimulus x action average of the eye movements
% This helps assess whether the eyes follow the stim, or the eyes follow
% the navigation!
minLengths = zeros(size(stimActionEyeMoveTrials,1), size(stimActionEyeMoveTrials,2), 1);
resampledStimActionEye = cell(size(stimActionEyeMoveTrials));
m = cell(size(stimActionEyeMoveTrials));
sem = cell(size(stimActionEyeMoveTrials));
for i=1:numStim
    for j=1:numStim
        h = [];
        if (~isempty(stimActionEyeMoveTrials{i,j,1}))
            stimActionEyeLengths = cellfun(@(x) length(x), stimActionEyeMoveTrials{i,j,1});
            minLengths(i,j) = min(stimActionEyeLengths);  % Use min instead of max, as max adds some artifacts at the end, and both give the same shape
            resampledStimActionEye{i,j,1} = cellfun(@(x) resample(x, minLengths(i,j), length(x)), stimActionEyeMoveTrials{i,j,1}, 'UniformOutput', false);
            d = cell2mat(resampledStimActionEye{i,j,1}(:)');
            m{i,j,1} = nanmean(d, 2);
            sem{i,j,1} = nanstd(d, [], 2) ./ sqrt(size(d, 1));
            resampledStimActionEye{i,j,2} = cellfun(@(x) resample(x, minLengths(i,j), length(x)), stimActionEyeMoveTrials{i,j,2}, 'UniformOutput', false);
            d = cell2mat(resampledStimActionEye{i,j,2}(:)');
            m{i,j,2} = nanmean(d, 2);
            sem{i,j,2} = nanstd(d, [], 2) ./ sqrt(size(d, 1));

            figure; hold on;
            x = 1:length(m{i,j,1});
            xSem = cat(2, x, fliplr(x));
            ySem1 = [m{i,j,1}'-sem{i,j,1}', fliplr(m{i,j,1}'+sem{i,j,1}')];
            ySem2 = [m{i,j,2}'-sem{i,j,2}', fliplr(m{i,j,2}'+sem{i,j,2}')];
            patch(ySem1, xSem, leftEyeVarianceColor, 'EdgeColor', 'none');
            alpha(varianceAlpha);
            patch(ySem2, xSem, rightEyeVarianceColor, 'EdgeColor', 'none');
            alpha(varianceAlpha);
            h = [h plot(m{i,j,1}, x, 'Color', leftEyeColor, 'LineWidth', lw)];
            h = [h plot(m{i,j,2}, x, 'Color', rightEyeColor, 'LineWidth', lw)];
            plot(zeros(1,length(m{i,j,1})), x, 'k--'); 
            if (numStim == 3)
                if (i == 1)
                    sLoc = 'Left';
                elseif (i == 2)
                    sLoc = 'Right';
                else
                    sLoc = 'Center';
                end
                if (j == 1)
                    aLoc = 'Left';
                elseif (j == 2)
                    aLoc = 'Right';
                else
                    aLoc = 'Center';
                end
            else
                if (i == 1)
                    sLoc = 'Left near';
                elseif (i == 2)
                    sLoc = 'Left far';
                elseif (i == 3)
                    sLoc = 'Right near';
                else
                    sLoc = 'Right far';
                end
                if (j == 1)
                    aLoc = 'Left near';
                elseif (j == 2)
                    aLoc = 'Left far';
                elseif (j == 3)
                    aLoc = 'Right near';    
                else
                    aLoc = 'Right far';
                end
            end
            title([rootFileName ': ' sLoc ' stim / ' aLoc ' action'], 'Interpreter', 'none');
            annotation('textbox', [.8 0 .2 .2], 'String', ['n=' num2str(length(stimActionEyeMoveTrials{i,j,1}))], 'FitBoxToText', 'on', 'EdgeColor', 'white');  
            legend(h(1:2), 'left eye', 'right eye');
            xlim([-10 10]);
            ylim([0 length(x)]);
            ylabel('Frame (normalized)')
            xlabel('Pupil Azimuth (deg)');
        end
    end
end

% Fifth, if there was optogenetics, plot, for each eye, the effect of optogenetics on the eye movements for a given stimulus.
%  I expect that the opto state simple makes the contralateral stimulus
%  invisible and the mouse's eyes will simply act as they do on the
%  center-stimulus task.  This is what I have seen so far.
if(~isempty(find(optoStates > optoNone, 1))) % If opto experiment data, plot this as well
    minLengths = zeros(numStim, 1);  % 1 for each stimulus location
    for (k=1:2) % For each eye
        resampledStimOptoEye = cell(size(stimOptoEyeMoveTrials));
        m = cell(size(stimOptoEyeMoveTrials));
        sem = cell(size(stimOptoEyeMoveTrials));
        ySem = cell(size(stimOptoEyeMoveTrials));
        stimOptoEyeLengths = [];
        for i=1:numStim
            n = 0;
            leg = {};
            h = [];
            % Make part of label for figure
            if (numStim == 3)
               if (i == 1)
                   sLoc = 'Left';
               elseif (i == 2)
                   sLoc = 'Right';
               else
                   sLoc = 'Center';
               end
            elseif (numStim == 4)
               if (i == 1)
                   sLoc = 'Left Near';
               elseif (i == 2)
                   sLoc = 'Left Far';
               elseif (i == 3)
                   sLoc = 'Right Near';
               elseif (i == 4)
                   sLoc = 'Right Far';
               end
            end
            
            for j=1:4  % 4 possible opto states
                if (~isempty(stimOptoEyeMoveTrials{i,j,k}))
                    stimOptoEyeLengths = [stimOptoEyeLengths cellfun(@(x) length(x), stimOptoEyeMoveTrials{i,j,k})];
                    n = n + length(stimOptoEyeMoveTrials{i,j});
                end
            end
            minLengths(i) = min(stimOptoEyeLengths);  % Use min instead of max, as max adds some artifacts at the end, and both give the same shape

            % Now calculate the mean and sem for each opto state for this
            % stim, and plot it on a single graph
            figure; hold on;
            first = 1;
            for j=1:4 % 4 possible opto states: OFF, LEFT, RIGHT or BOTH
                if (~isempty(stimOptoEyeMoveTrials{i,j,k}))
                    if (j == 1)
                        os = 'No light';
                    elseif (j == 2)
                        os = 'Left cortex light';
                    elseif (j == 3)
                        os = 'Right cortex light';
                    elseif (j == 4)
                        os = 'Both cortices light';
                    end
                    leg = cat(1, leg, os);
                    resampledStimOptoEye{i,j,k} = cellfun(@(x) resample(x, minLengths(i), length(x)), stimOptoEyeMoveTrials{i,j,k}, 'UniformOutput', false);
                    d = cell2mat(resampledStimOptoEye{i,j,k}(:)');
                    m{i,j,k} = nanmean(d, 2);
                    sem{i,j,k} = nanstd(d, [], 2) ./ sqrt(size(d, 1));

                    x = 1:length(m{i,j,k}); % All the lengths are the same for each optostate, so just pull from 1
                    if (first)
                        plot(x, zeros(1,length(x)), 'k--');
                        first = 0;
                    end
                    xSem = cat(2, x, fliplr(x));
                    ySem{i,j} = [m{i,j,k}'-sem{i,j,k}', fliplr(m{i,j,k}'+sem{i,j,k}')];
                    curColor = colorOpto(j,:);
                    patch(ySem{i,j}, xSem, curColor, 'EdgeColor', 'none');
                    alpha(optoVarianceAlpha);
                    h = [h plot(m{i,j,k}, x, 'Color', curColor, 'LineWidth', lw)];
                end
            end
            
            if (k == 1)
                eye = 'Left';
            else
                eye = 'Right';
            end

            title([rootFileName ': ' sLoc ' stim, ' eye ' eye position'], 'Interpreter', 'none');
            annotation('textbox', [.8 0 .2 .2], 'String', ['n=' num2str(n)], 'FitBoxToText', 'on', 'EdgeColor', 'white');  
            legend(h, leg);
            ylim([-10 10]);
            xlim([0 length(x)]);
            xlabel('Frame (normalized)')
            ylabel('Pupil Azimuth (deg)');
        end
    end
end
%}

% Plot stimulation and actions first
% Then plot inter-trial intervals
% Finally plot actual eye movement data

xUnitsTime = frameStart:frameStop;
xTrialsUnitsTime = xTrials;
xPauseUnitsTime = xPause;
xlab = 'time (frames)';
if (timeDivider > 1)
    xUnitsTime = xUnitsTime / timeDivider;
    xTrialsUnitsTime = xTrialsUnitsTime / timeDivider;
    xPauseUnitsTime = xPauseUnitsTime / timeDivider;
    if (timeDivider == 60*60)
        xlab = 'time (min)';
    elseif (timeDivider == 60)
        xlab = 'time (sec)';
    end
end

% ELEVATION PLOT
figure; hold on
patch(xTrialsUnitsTime, yStim, stimPatchColors, 'EdgeColor', 'none');
patch(xTrialsUnitsTime, yAction, actionPatchColors, 'EdgeColor', 'none');
if(~isempty(find(optoStates > optoNone, 1))) % If opto experiment data, mark optostate on the graph
    patch(xTrialsUnitsTime, yOpto, optoColors, 'EdgeColor', 'none');
end
if (~isempty(trialStartFrames))
    patch(xPauseUnitsTime, yPause, shadingColorInterTrial, 'EdgeColor', shadingColorInterTrial);
end
h = plot(xUnitsTime, elavDeg(1:length(xUnitsTime), 1), leftEyeColor, xUnitsTime, elavDeg(1:length(xUnitsTime), 2), rightEyeColor);
title([rootFileName ': Pupil Elevation'], 'Interpreter', 'none');
ylabel('Pupil Elevation (deg)');
xlabel(xlab);
for i = 1:size(legendColors, 1)
    p(i) = patch(NaN, NaN, legendColors(i,:));
end
if (length(numStim) == 1)
    if (numStim(1) == 1)
        legend([h;p'], 'left eye', 'right eye', 'straight stim/action', 'catch trial', 'inter-trial interval');
    elseif (numStim(1) == 2)
        legend([h;p'], 'left eye', 'right eye', 'left stim/action', 'right stim/action', 'catch trial', 'inter-trial interval');
    elseif (numStim(1) == 3)
        legend([h;p'], 'left eye', 'right eye', 'left stim/action', 'right stim/action', 'straight stim/action', ...
                        'catch trial', 'inter-trial interval');
    elseif (numStim(1) == 4)
        legend([h;p'], 'left eye', 'right eye', 'left near stim/action', 'right near stim/action', 'left far stim/action', ...
                        'right far stim/action', 'catch trial', 'inter-trial interval');
    end
elseif (length(numStim) == 2)
    if (numStim(1) == 3)
        if (numStim(2) == 3)
            legend([h;p'], 'left eye', 'right eye', 'left near stim/action', 'left far stim/action', ...
                           'right near stim/action', 'right far stim/action', 'straight stim/action', ...
                           'catch trial', 'inter-trial interval');
        end
    elseif (numStim(1) == 2)
        if (numStim(2) == 2)
            legend([h;p'], 'left eye', 'right eye', 'left near stim/action', 'left far stim/action', ...
                           'right near stim/action', 'right far stim/action', 'catch trial', 'inter-trial interval');
        end
    end
end
% Add catch to legends above?
ylim([ymin/2 ymax/2]);
xlim([0 xUnitsTime(end)]);
dcmObj = datacursormode(gcf);
set(dcmObj,'UpdateFcn',@dataCursorCallback,'Enable','on');

% AZIMUTH PLOT
figure; hold on
patch(-yStim, xTrialsUnitsTime, stimPatchColors, 'EdgeColor', 'none');
patch(-yAction, xTrialsUnitsTime, actionPatchColors, 'EdgeColor', 'none');
if(~isempty(find(optoStates > optoNone, 1))) % If opto experiment data, mark optostate on the graph
    patch(yOpto, xTrialsUnitsTime, optoColors, 'EdgeColor', 'none');
end
if (~isempty(trialStartFrames))
    patch(yPause, xPauseUnitsTime, shadingColorInterTrial, 'EdgeColor', shadingColorInterTrial);
end
h = plot(azimDeg(1:length(xUnitsTime), 1), xUnitsTime, leftEyeColor, azimDeg(1:length(xUnitsTime), 2), xUnitsTime, rightEyeColor);
title([rootFileName ': Pupil Azimuth'], 'Interpreter', 'none');
xlabel('Pupil Azimuth (deg)');
ylabel(xlab);
for i = 1:size(legendColors,1)
    p(i) = patch(NaN, NaN, legendColors(i,:));
end
if (length(numStim) == 1)
    if (numStim(1) == 1)
        legend([h;p'], 'left eye', 'right eye', 'straight stim/action', 'catch trial', 'inter-trial interval');
    elseif (numStim(1) == 2)
        legend([h;p'], 'left eye', 'right eye', 'left stim/action', 'right stim/action', 'catch trial', 'inter-trial interval');
    elseif (numStim(1) == 3)
        legend([h;p'], 'left eye', 'right eye', 'left stim/action', 'right stim/action', 'straight stim/action', ...
                        'catch trial', 'inter-trial interval');
    elseif (numStim(1) == 4)
        legend([h;p'], 'left eye', 'right eye', 'left near stim/action', 'right near stim/action', 'left far stim/action', ...
                        'right far stim/action', 'catch trial', 'inter-trial interval');
    end
elseif (length(numStim) == 2)
    if (numStim(1) == 3)
        if (numStim(2) == 3)
            legend([h;p'], 'left eye', 'right eye', 'left near stim/action', 'left far stim/action', ...
                           'right near stim/action', 'right far stim/action', 'straight stim/action', ...
                           'catch trial', 'inter-trial interval');
        end
    elseif (numStim(1) == 2)
        if (numStim(2) == 2)
            legend([h;p'], 'left eye', 'right eye', 'left near stim/action', 'left far stim/action', ...
                           'right near stim/action', 'right far stim/action', 'catch trial', 'inter-trial interval');
        end
    end
end
% Add catch to legends above?
xlim([ymin ymax]);
ylim([0 xUnitsTime(end)]);
dcmObj = datacursormode(gcf);
set(dcmObj,'UpdateFcn',@dataCursorCallback,'Enable','on');

% Finally, calculate the amplitudes of all saccades observed.  To start, we will just analyze the azimuth trace.
% First, if there are NaNs in each eye trace, interpolate to fill in the data and remove all NaNs.
% The code needs to annotate saccadeStart and saccadeEnd for each saccade.  Those are specific frames.
% Then, a set of amplitudes can easily be extracted from these sets.
azimDegNoNaN = azimDeg;
saccadeStartFrames = cell(1,2);
saccadeEndFrames = cell(1,2);
saccadeAmplitudes = cell(1,2);
saccadeCountsIntoField = zeros(2,2);  % First row is left eye, second row is right eye; first column is left field, second column is right field
saccadeThresh = zeros(1,2);
for i=1:size(azimDeg,2) % for each eye, fill in the NaNs
    % if 1 eye is not visible, skip it!
    if (sum(isnan(azimDeg(:,i))) == length(azimDeg(:,i)))
        continue;
    end
    nanAz = isnan(azimDeg(:,i));
    t = 1:numel(azimDeg(:,i));
    azimDegNoNaN(nanAz,i) = interp1(t(~nanAz), azimDegNoNaN(~nanAz,i), t(nanAz));
    % Set any remaining NaNs, like at the beginning or end, to the nearest value.  There is probably a cleaner way to do this... But this works.
    nanIdx = find(isnan(azimDegNoNaN(:,i)));
    if (~isempty(nanIdx))
        firstNaN = nanIdx(1);
        lastNaN = nanIdx(end);
        if firstNaN == 1 % Array starts with a NaN, so that failed to interpolate
            firstNoNaN = find(~isnan(azimDegNoNaN(:,i)), 1);
            azimDegNoNaN(1:firstNoNaN-1, i) = azimDegNoNaN(firstNoNaN, i);
        end
        if lastNaN == length(azimDegNoNaN(:,i)) % Array ends with a NaN
            lastNoNaN = find(~isnan(azimDegNoNaN(:,i)), 1, 'last');
            azimDegNoNaN(lastNoNaN+1:end, i) = azimDegNoNaN(lastNoNaN, i);
        end
    end
        
    d = diff(azimDegNoNaN(:,i));
    saccadeThresh(i)= 3 * std(d);  % std(d) is often near 1 deg - was 2x std, just changed to 3x
    f = find(abs(d) > saccadeThresh(i));
    fd = diff(f);
    saccadeStartFrames{i} = [f(1); f(find(fd ~= 1)+1)];
    saccadeEndFrames{i} = f(fd ~= 1)+1;
    if (length(saccadeEndFrames{i}) < length(saccadeStartFrames{i}))  % Find the end of the last saccade
        saccadeEndFrames{i}(end+1) = f(end) + 1;
    end
    % Now, for each saccade pair, store its amplitude
    % For now, assume the saccade start and end frames are the largest swing, but this might not be true so later examine the full range for min and max locations
    saccadeAmplitudes{i} = azimDegNoNaN(saccadeEndFrames{i},i) - azimDegNoNaN(saccadeStartFrames{i},i);
    % Also, if the endFrame ends in the left or right field, count it as being into the left or right field.
    saccadeCountsIntoField(i, 1) = length(find(azimDegNoNaN(saccadeEndFrames{i}, i) < -saccadeThresh(i)));
    saccadeCountsIntoField(i, 2) = length(find(azimDegNoNaN(saccadeEndFrames{i}, i) > saccadeThresh(i)));
    figure;
    histogram(saccadeAmplitudes{i}, -40:2:40);
    if (i == 1)
        title([rootFileName ': Saccades (left eye)'], 'Interpreter', 'none');
    elseif (i == 2)
        title([rootFileName ': Saccades (right eye)'], 'Interpreter', 'none');
    end
    xlabel('Saccade size (degrees)');
    ylabel('Count');
    % Also just plot the distribution of all saccade positions
    figure;
    histogram(azimDegNoNaN(:,i), -40:2:40);
    if (i == 1)
        title([rootFileName ': Pupil position (left eye)'], 'Interpreter', 'none');
    elseif (i == 2)
        title([rootFileName ': Pupil position (right eye)'], 'Interpreter', 'none');
    end
    xlabel('Azimuth (degrees)');
    ylabel('Count');
end

% Save it all for posterity
save([trackFileName(1:end-4) '_an.mat'], 'centers', 'areas', 'elavDeg', 'azimDeg', 'trialStartFrames', ...
    'trialEndFrames', 'stimIdxs', 'actionIdxs', 'optoStates', 'worldIdxs', 'eyeblinkStartFrames', ...
    'azimDegNoNaN', 'saccadeStartFrames', 'saccadeEndFrames', 'saccadeAmplitudes', 'saccadeThresh', 'areasMm2', 'Rp', ...
    'slope', 'yint');

saccadeAmplitudesLEye = saccadeAmplitudes{1};
saccadeAmplitudesREye = saccadeAmplitudes{2};

disp(['L Eye: Saccade threshold = ' num2str(saccadeThresh(1))]);
disp(['L Eye: Mean saccade amplitude = ' num2str(mean(abs(saccadeAmplitudes{1})))]);
disp(['L Eye: Num saccades into left field = ' num2str(saccadeCountsIntoField(1,1))]);
disp(['L Eye: Num saccades into right field= ' num2str(saccadeCountsIntoField(1,2))]);
disp(['Ratio of R/L = ' num2str(saccadeCountsIntoField(1,2) / saccadeCountsIntoField(1,1))]);
disp(['R Eye: Saccade threshold = ' num2str(saccadeThresh(2))]);
disp(['R Eye: Mean saccade amplitude = ' num2str(mean(abs(saccadeAmplitudes{2})))]);
disp(['R Eye: Num saccades into left field = ' num2str(saccadeCountsIntoField(2,1))]);
disp(['R Eye: Num saccades into right field= ' num2str(saccadeCountsIntoField(2,2))]);
disp(['Ratio of R/L = ' num2str(saccadeCountsIntoField(2,2) / saccadeCountsIntoField(2,1))]);

%disp(['Mean pupil sizes by areas: L = ' num2str(round(sqrt(nanmean(areasMm2(:,1,1))/pi)*2, 2)) ' mm diameter, R = ' ...
%                                num2str(round(sqrt(nanmean(areasMm2(:,1,2))/pi)*2, 2)) ' mm diameter']);

disp(['Mean pupil sizes by ellipse major axis: L = ' num2str(round(nanmean(majorAxisMm(:,1,1)), 2)) ' mm diameter, R = ' ...
                                num2str(round(nanmean(majorAxisMm(:,1,2)), 2)) ' mm diameter']);

disp(['Mean Rp-L = ' num2str(nanmean(RpL)) ', Rp-R = ' num2str(nanmean(RpR)) ]);                            
                            
plotTargetAzimLoc(mouseName, day, [1 0], [], 1, 'L', 0, 1, 1, [0 0], 1, 1, 1, 0, 0, 0);
plotTargetAzimLoc(mouseName, day, [1 0], [], 1, 'R', 0, 1, 1, [0 0], 1, 1, 1, 0, 0, 0);

end