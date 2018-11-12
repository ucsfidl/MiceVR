function analyzePupils(trackFileName, numStim, frameLim, degPerPx, fps)
% Once trackPupils is done, this script is used to analyze the pupil
% positions and plot a bunch of stuff.

% ISSUE: because frames are dropped to determine when a trial starts and
% ends, the overall time plotted will be lagging more and more over time.
% Fix this at some point if it is important.

% > analyzePupils('Zizzle-D58-nb_04_sw3_wt-S53_trk.mat', 4, [1 0], 0.98, 60)

tic;

%leftColor = [1 1 0.79];  % off-yellow
%rightColor = [0.81 1 0.81];  % off-green

leftEyeColor = 'b'; 
rightEyeColor = 'r';
leftEyeVarianceColor = [0.74 0.87 1];  % light blue
rightEyeVarianceColor = [1 0.77 0.84]; % light red
varianceAlpha = 0.5;

colorLeft = [2 87 194]/255;  % blue
colorRight = [226 50 50]/255; % red
colorCenter = [15 157 88]/255;  % green

shadingColorLeft = [0.84 0.89 0.99];  % dull blue
shadingColorLeftFar = [0.84 0.98 0.99]; % dull cyan
shadingColorRight = [1 0.87 0.71]; % dull orange
shadingColorRightFar = [1 1 0.79];  % dull yellow
shadingColorCenter = [0.85 1 0.8]; % dull green
%colorCenter = [1 0.9 0.99];  % dull purple
shadingColorInterTrial = [0.9 0.9 0.9];  % grey

ymin = -25;
ymax = 25;

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
         
if (numStim == 4)
    allColors = [shadingColorLeft; shadingColorLeftFar; shadingColorRight; shadingColorRightFar; shadingColorInterTrial];
elseif (numStim == 3)
    allColors = [shadingColorLeft; shadingColorRight; shadingColorCenter; shadingColorInterTrial];
end

frameStart = frameLim(1);
frameStop = frameLim(2);

% Be sure to change these if the x location of the trees changes
stimLeftNear = 19977;
stimLeftFar = 19978;
stimRightNear = 20023;
stimRightFar = 20022;

stimCenter = 20000;

trialStartOffset = 1;  % Add this much to the recorded trial frame starts - for backwards compatibility
trialEndOffset = 0;

load(trackFileName, 'centers', 'areas');
if (isempty(strfind(trackFileName, '_part_trk.mat')))
    rootFileName = trackFileName(1:end-8);
else
    rootFileName = trackFileName(1:end-13);
end    

totalFrames = length(centers);

if frameStop == 0 || frameStop > totalFrames
    frameStop = totalFrames;
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
optoStates = [];
actionsFileName = [rootFileName '_actions.txt'];
actionsFile = fopen(actionsFileName);
if (actionsFile ~= -1) % File found
    fgetl(actionsFile);  % First line is a header so ignore
    % Special processing here since I changed the format of the log files
    % to make them not backwards compatible after June 4, 2018.  All future
    % changes should be backwards compatible.
    [dum, str] = dos(['dir ' actionsFileName]);
    c = textscan(str,'%s');
    createdate = c{1}{15};
    cd = datetime(createdate, 'InputFormat', 'MM/dd/yyyy');
    sd = datetime('06/04/2018', 'InputFormat', 'MM/dd/yyyy');
    if (cd <= sd)  % For backwards compatibility
        expr = '.*?\t.*?\t.*?\t.*?\t(.*?)\t.*?\t.*?\t(.*?)\t'; % The last frame of each trial is in the 3rd column in the actions file
    else
        expr = '.*?\t.*?\t.*?\t.*?\t(.*?)\t.*?\t.*?\t.*?\t.*?\t.*?\t.*?\t(.*?)\t.*?\t.*?\t.*?\t.*?\t([^\s]*)\t*';
    end
    m=0;
    while(true)
        if (m == 94)
            %disp(m);
        end
        m = m+1;
        line = fgetl(actionsFile);
        if (line ~= -1) % The file is not finished
            tokens = regexp(line, expr, 'tokens');
            stimLocs = [stimLocs str2num(tokens{1}{1})];
            actionLocs = [actionLocs str2num(tokens{1}{2})];
            if (length(tokens{1}) > 2)
                optoStates = [optoStates str2num(tokens{1}{3})];
            else
                optoStates = [optoStates optoNone];  % Populate with the value of no opto
            end
        else
            break;
        end
    end
else
    error('No actions file found.  Be sure the name matches the track file name.');
end

% If trial times are available, incorporate into the graphs
load([rootFileName '.mat'], 'trialStarts', 'trialEnds');
if (trialStarts(1).FrameNumber ~= 0)  % new format includes 1, but old format didn't, so permit backwards compatibility
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

numCompletedTrials = length(stimLocs);

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
if (trimmedEnds(end) < frameStop && frameStop ~= totalFrames)
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

stimColors = zeros(1, numCompletedTrials, 3);  % Stim location colors to to use when plotting
actionColors = zeros(1, numCompletedTrials, 3);
optoColors = zeros(1, numCompletedTrials, 3);

if (numStim == 4)
    for i=1:numCompletedTrials
        if (stimLocs(i) == stimLeftNear)
            stimColors(1,i,:) = shadingColorLeft;
            idx = 1;
        elseif (stimLocs(i) == stimLeftFar)
            stimColors(1,i,:) = shadingColorLeftFar;
            idx = 2;
        elseif (stimLocs(i) == stimRightNear)
            stimColors(1,i,:) = shadingColorRight;
            idx = 3;
        elseif (stimLocs(i) == stimRightFar)
            stimColors(1,i,:) = shadingColorRightFar;            
            idx = 4;
        end
        
        if (actionLocs(i) == stimLeftNear)
            actionColors(1,i,:) = shadingColorLeft;
            idx = 1;
        elseif (actionLocs(i) == stimLeftFar)
            actionColors(1,i,:) = shadingColorLeftFar;
            idx = 2;
        elseif (actionLocs(i) == stimRightNear)
            actionColors(1,i,:) = shadingColorRight;
            idx = 3;
        elseif (actionLocs(i) == stimRightFar)
            actionColors(1,i,:) = shadingColorRightFar;
            idx = 4;
        end
        
        optoColors(1,i,:) = colorOpto(optoStates(i) + 2,:);
    end
elseif (numStim == 3)
    for i=1:numCompletedTrials
        if (stimLocs(i) < stimCenter)
            stimColors(1,i,:) = shadingColorLeft;
            idx = 1;
        elseif (stimLocs(i) > stimCenter)
            stimColors(1,i,:) = shadingColorRight;
            idx = 2;
        else
            stimColors(1,i,:) = shadingColorCenter;
            idx = 3;
        end
        
        if (actionLocs(i) < stimCenter)
            actionColors(1,i,:) = shadingColorLeft;
            idx = 1;
        elseif (actionLocs(i) > stimCenter)
            actionColors(1,i,:) = shadingColorRight;
            idx = 2;
        else
            actionColors(1,i,:) = shadingColorCenter;
            idx = 3;
        end
        
        optoColors(1,i,:) = colorOpto(optoStates(i) + 2,:);
    end
end

stimColors = stimColors(1, 1+numTruncatedStart:end-numTruncatedEnd, :);
actionColors = actionColors(1, 1+numTruncatedStart:end-numTruncatedEnd, :);

% Collect data for averaging...
trimmedStimLocs = stimLocs(1+numTruncatedStart:end-numTruncatedEnd);
trimmedActionLocs = actionLocs(1+numTruncatedStart:end-numTruncatedEnd);
stimEyeMoveTrials = cell(numStim, 2);
actionEyeMoveTrials = cell(numStim, 2);
stimActionEyeMoveTrials = cell(numStim, numStim, 2);  % first axis is stimLoc, second is actionLoc, third is left/right eye
stimOptoEyeMoveTrials = cell(numStim, 4, 2); % first axis is stimLoc, second is optoState (OFF, LEFT, RIGHT, or BOTH), third is left/right eye
for i=1:length(trimmedStimLocs)
    if (numStim == 4)
        if (trimmedStimLocs(i) == stimLeftNear)
            idx1 = 1;
        elseif (trimmedStimLocs(i) == stimLeftFar)
            idx1 = 2;
        elseif (trimmedStimLocs(i) == stimRightNear)
            idx1 = 3;
        elseif (trimmedStimLocs(i) == stimRightFar)
            idx1 = 4;
        end
    elseif (numStim == 3)
        if (trimmedStimLocs(i) < stimCenter)
            idx1 = 1;
        elseif (trimmedStimLocs(i) > stimCenter)
            idx1 = 2;
        elseif (trimmedStimLocs(i) == stimCenter)
            idx1 = 3;
        end        
    end
    for j=1:2
        s = stimEyeMoveTrials{idx1, j};
        s{end+1} = azimDeg(trimmedStarts(i):trimmedEnds(i)-1, j);
        stimEyeMoveTrials{idx1, j} = s;
    end

    if (numStim == 4)
        if (trimmedActionLocs(i) == stimLeftNear)
            idx2 = 1;
        elseif (trimmedActionLocs(i) == stimLeftFar)
            idx2 = 2;
        elseif (trimmedActionLocs(i) == stimRightNear)
            idx2 = 3;
        elseif (trimmedActionLocs(i) == stimRightFar)
            idx2 = 4;
        end
    elseif (numStim == 3)
        if (trimmedActionLocs(i) < stimCenter)
            idx2 = 1;
        elseif (trimmedActionLocs(i) > stimCenter)
            idx2 = 2;
        elseif (trimmedActionLocs(i) == stimCenter)
            idx2 = 3;
        end
    end
    for j=1:2
        a = actionEyeMoveTrials{idx2, j};
        a{end+1} = azimDeg(trimmedStarts(i):trimmedEnds(i)-1, j);
        actionEyeMoveTrials{idx2, j} = a;
    end
    
    for j=1:2
        sa = stimActionEyeMoveTrials{idx1, idx2, j};
        sa{end+1} = azimDeg(trimmedStarts(i):trimmedEnds(i)-1, j);
        stimActionEyeMoveTrials{idx1, idx2, j} = sa;
    end
    
    for j=1:2
        osIdx = optoStates(i) + 2;  % Shift from the read value {-1,2) to the index (1,4)
        so = stimOptoEyeMoveTrials{idx1, osIdx, j};
        so{end+1} = azimDeg(trimmedStarts(i):trimmedEnds(i)-1, j);
        stimOptoEyeMoveTrials{idx1, osIdx, j} = so;
    end
end

% Now, normalize by resampling, and then plot the average of the resampled eye movements.
minLengths = zeros(2, 1);  % One for each eye
% First, plot the stimulus average of the eye movements
for j=1:2  % For each eye
    resampledStimEye = cell(size(stimEyeMoveTrials));
    m = cell(size(stimEyeMoveTrials));
    sem = cell(size(stimEyeMoveTrials));
    ySem = cell(size(stimEyeMoveTrials));
    h = [];
    n = 0;
    stimEyeLengths = [];
    for i=1:numStim
        stimEyeLengths = [stimEyeLengths cellfun(@(x) length(x), stimEyeMoveTrials{i,j})];
        n = n + length(stimEyeMoveTrials{i,j});
    end
    minLengths(j) = min(stimEyeLengths);  % Use min instead of max, as max adds some artifacts at the end, and both give the same shape
    
    for i=1:numStim
        resampledStimEye{i,j} = cellfun(@(x) resample(x, minLengths(j), length(x)), stimEyeMoveTrials{i,j}, 'UniformOutput', false);
        d = cell2mat(resampledStimEye{i,j}(:)');
        m{i,j} = nanmean(d, 2);
        sem{i,j} = nanstd(d, [], 2) ./ sqrt(size(d, 1));
    end

    for i=1:numStim
        ySem{i,j} = [m{i,j}'-sem{i,j}', fliplr(m{i,j}'+sem{i,j}')];
    end
    
    figure; hold on;
    x = 1:length(m{1,j}); % All the lengths are the same for each stim for 1 eye, so just pull from 1
    plot(x, zeros(1,length(m{i,j})), 'k--');
    xSem = cat(2, x, fliplr(x));
    for i=1:numStim
        if (numStim == 3)
            if (i==1)
                curColor = colorLeft;
                curShadingColor = shadingColorLeft;
                sLoc = 'Left';
            elseif (i==2)
                curColor = colorRight;
                curShadingColor = shadingColorRight;
                sLoc = 'Right';
            elseif (i==3)
                curColor = colorCenter;
                curShadingColor = shadingColorCenter;
                sLoc = 'Center';
            end
        elseif (numStim == 4)
            if (i==1)
                curColor = colorLeft;
                curShadingColor = shadingColorLeft;
                sLoc = 'Left Near';
            elseif (i==2)
                curColor = colorLeftFar;
                curShadingColor = shadingColorLeftFar;
                sLoc = 'Left Far';
            elseif (i==3)
                curColor = colorRight;
                curShadingColor = shadingColorRight;
                sLoc = 'Right Near';
            elseif (i==4)
                curColor = colorRightFar;
                curShadingColor = shadingColorRightFar;
                sLoc = 'Right Far';
            end
        end
        patch(xSem, ySem{i,j}, curShadingColor, 'EdgeColor', 'none');
        alpha(varianceAlpha);
        h = [h plot(x, m{i,j}, 'Color', curColor, 'LineWidth', lw)];
    end
    if (j == 1)
        eye = 'Left';
    else
        eye = 'Right';
    end

    title([rootFileName ': ' eye ' eye'], 'Interpreter', 'none');
    annotation('textbox', [.8 0 .2 .2], 'String', ['n=' num2str(n)], 'FitBoxToText', 'on', 'EdgeColor', 'white');  
    if (numStim == 3)
        legend(h, 'left stim', 'right stim', 'center stim');
    elseif (numStim == 4)
        legend(h, 'left near stim', 'left far stim', 'right near stim', 'right far stim');
    end
    ylim([-10 10]);
    xlim([0 length(x)]);
    xlabel('Frame (normalized)')
    ylabel('azimuth (deg, + right, - left)');
end

% Second, plot the stimulus x action average of the eye movements
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
            patch(xSem, ySem1, leftEyeVarianceColor, 'EdgeColor', 'none');
            alpha(varianceAlpha);
            patch(xSem, ySem2, rightEyeVarianceColor, 'EdgeColor', 'none');
            alpha(varianceAlpha);
            h = [h plot(x, m{i,j,1}, 'Color', leftEyeColor, 'LineWidth', lw)];
            h = [h plot(x, m{i,j,2}, 'Color', rightEyeColor, 'LineWidth', lw)];
            plot(x, zeros(1,length(m{i,j,1})), 'k--'); 
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
            ylim([-10 10]);
            xlim([0 length(x)]);
            xlabel('Frame (normalized)')
            ylabel('azimuth (deg, + right, - left)');
        end
    end
end

% Third, if there was optogenetics, plot, for each eye, the effect of optogenetics on the eye movements for a given stimulus.
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
                    patch(xSem, ySem{i,j}, curColor, 'EdgeColor', 'none');
                    alpha(optoVarianceAlpha);
                    h = [h plot(x, m{i,j,k}, 'Color', curColor, 'LineWidth', lw)];
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
            ylabel('azimuth (deg, + right, - left)');
        end
    end
end


save([trackFileName(1:end-4) '_an.mat'], 'centers', 'areas', 'elavDeg', 'azimDeg', 'trialStartFrames', 'trialEndFrames', 'stimLocs', 'actionLocs', 'optoStates');


% Plot stimulation and actions first
% Then plot inter-trial intervals
% Finally plot actual eye movement data

xUnitsTime = (frameStart:frameStop)/fps;
xTrialsUnitsTime = xTrials/fps;
xPauseUnitsTime = xPause/fps;

% ELEVATION PLOT
figure; hold on
patch(xTrialsUnitsTime, yStim, stimColors, 'EdgeColor', 'none');
patch(xTrialsUnitsTime, yAction, actionColors, 'EdgeColor', 'none');
if(~isempty(find(optoStates > optoNone, 1))) % If opto experiment data, mark optostate on the graph
    patch(xTrialsUnitsTime, yOpto, optoColors, 'EdgeColor', 'none');
end
if (~isempty(trialStartFrames))
    patch(xPauseUnitsTime, yPause, shadingColorInterTrial, 'EdgeColor', shadingColorInterTrial);
end
h = plot(xUnitsTime, elavDeg(:, 1), leftEyeColor, xUnitsTime, elavDeg(:, 2), rightEyeColor);
title([rootFileName ': Pupil Elevation'], 'Interpreter', 'none');
ylabel('elevation (deg, + right, - left)');
xlabel('time (s)');
for i = 1:length(allColors)
    p(i) = patch(NaN, NaN, allColors(i,:));
end
if (numStim == 4)
    legend([h;p'], 'left eye', 'right eye', 'left near stim/action', 'left far stim/action', 'right near stim/action', 'right far stim/action', 'inter-trial interval');
elseif (numStim == 3)
    legend([h;p'], 'left eye', 'right eye', 'left stim/action', 'right stim/action', 'center stim/action', 'inter-trial interval');
end
ylim([ymin ymax]);
xlim([0 xUnitsTime(end)]);
dcmObj = datacursormode(gcf);
set(dcmObj,'UpdateFcn',@dataCursorCallback,'Enable','on');

% AZIMUTH PLOT
figure; hold on
patch(xTrialsUnitsTime, yStim, stimColors, 'EdgeColor', 'none');
patch(xTrialsUnitsTime, yAction, actionColors, 'EdgeColor', 'none');
if(~isempty(find(optoStates > optoNone, 1))) % If opto experiment data, mark optostate on the graph
    patch(xTrialsUnitsTime, yOpto, optoColors, 'EdgeColor', 'none');
end
if (~isempty(trialStartFrames))
    patch(xPauseUnitsTime, yPause, shadingColorInterTrial, 'EdgeColor', shadingColorInterTrial);
end
h = plot(xUnitsTime, azimDeg(:, 1), leftEyeColor, xUnitsTime, azimDeg(:, 2), rightEyeColor);
title([rootFileName ': Pupil Azimuth'], 'Interpreter', 'none');
ylabel('azimuth (deg, + right, - left)');
xlabel('time (s)');
for i = 1:length(allColors)
    p(i) = patch(NaN, NaN, allColors(i,:));
end
if (numStim == 4)
    legend([h;p'], 'left eye', 'right eye', 'left near stim/action', 'left far stim/action', 'right near stim/action', 'right far stim/action', 'inter-trial interval');
elseif (numStim == 3)
    legend([h;p'], 'left eye', 'right eye', 'left stim/action', 'right stim/action', 'center stim/action', 'inter-trial interval');
end
ylim([ymin ymax]);
xlim([0 xUnitsTime(end)]);
dcmObj = datacursormode(gcf);
set(dcmObj,'UpdateFcn',@dataCursorCallback,'Enable','on');

end