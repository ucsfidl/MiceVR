function [nasalExtremaL, nasalExtremaR, accuracyPerExtremaL, accuracyPerExtremaR] = ... 
    plotTargetAzimLoc(mouseName, days, trials, stimLocsToAnalyze, useFieldRestriction, whichEye, ...
    includeCorrectionTrials, includeCatchTrials, includeExtinctionTrials, ...
    targetAzimLimit, fractionOfRun, censorOnlyIfCorrect, outputNewActionsFile, writeOutOnlyIfCensored, ...
    interactive, verbose)
% This function takes as inputs the replay files for a session as well as the mouse's eye movements 
% during that session.  It outputs several plots:
% 1) For the specified trialType, a plot for each trial showing the 
%    azimuthal extent of the target.
% 2) An average plot of the azimuthal extent over all trials of a specific type
% 3) A plot with medial component of the azimuthal extent of the target
%    over all trials of a specific type, drawn on one plot

% The main point of this script is to help me determine whether the mice could be 
% cheating, that is, moving the ball and their eyes such that the target 
% enters the good field prior to deciding to run in that direction.

% So the most important trial type to plot is the one where the target
% is contralateral to the lesion.

% outputNewActionsFile set to 1 causes a new actions file to be written, which has the censored trials removed.

% targetAzimLimit is a tuple with the first element being for left targets and the 2nd for right targets

% EXAMPLE USAGE:
% >> [ne acc] = plotTargetAzimLoc('Uranus', [99],[],[1 0],[],[],1,1,1,'R',0, 1, [0 0], 0.57, 1, 0, 0);

% Old arguments that I don't change
sessions = [];
denoiseBallMovement = 1;
trialTypeStrArr = [];

%%% CHANGE THESE VARS FOR YOUR SETUP PRIOR TO RUNNING %%%
scenariosFolder = getScenariosFolder();
actionsFolder = getActionsFolder();
replaysFolder = getReplaysFolder();
eyevideosFolder = [pwd '\'];

actLineFormat = getActionLineFormat();

% Be sure to change these if the x location of the trees changes
stimLeftNear = 19973;
stimLeftFar = 19972;
stimRightNear = 20027;
stimRightFar = 20028;

stimLeft = 19975;
stimRight = 20025;
stimCenter = 20000;

shadingColorLeft = [0.84 0.89 0.99];  % dull blue
shadingColorLeftFar = [0.84 0.98 0.99]; % dull cyan
shadingColorRight = [1 0.87 0.71]; % dull orange
shadingColorRightFar = [0.9 0.9 0.69];  % dull yellow
shadingColorCenter = [0.85 1 0.8]; % dull green

grayShade = [0.95 0.95 0.95];

fps = 60;
successDelay = 2; %sec
failureDelay = 4; %sec
extraFramesAtStart = 2;  % 2 or 3: seems like there are 2 extra frames at the start - 122 frames of no motion instead of 120; need this for synchronizing with eye data
extraFramesAtEnd = 3; % 2 or 3: seems like there are a couple extra frames where the mouse is already teleported back to the beginning at the end
immobilePeriod = 2;  % 2 sec of immobility at the start of each trial

maxAllowedJump = 10;  % max allowed change in the target's azimuthal location between 2 frames, for removing noise and smoothening
                      % does not account for eye movements (which could be larger than this value), just target movements due
                      % to ball movements

nasalExtremaL = [];
nasalExtremaR = [];
totalTrialsAnalyzed = zeros(1,2);
totalExtinctTrialsAnalyzed = zeros(1,2);
totalTrialsNotAnalyzed = zeros(1,2);
totalExtinctTrialsNotAnalyzed = zeros(1,2);

% Variable that stores accuracy as a function of nasalExtrema
bucketSize = 5;  % pool accuracy data in 4 degree buckets
leftExtreme = -90;
rightExtreme = 90;
buckets = leftExtreme:bucketSize:rightExtreme;
buckets(1) = -Inf;
buckets(end) = Inf;
numCorrectL = zeros(length(buckets), 1);
numCorrectR = zeros(length(buckets), 1);
numIncorrectL = zeros(length(buckets), 1);
numIncorrectR = zeros(length(buckets), 1);

% Error out if number of sessions is non-zero and does not match number of days.
if (~isempty(sessions) && length(days) ~= length(sessions))
    error('Number of sessions is non-zero and does not match number of days. It should.')
end

% To start, let's just produce one plot of the first trial of a specified session.
% To do this, we need to align the eye movements within a trial with the 
% ball movements within that trial.  The replay file has the ball movements and the 
% angular location of the target and distractor, and the analyzed eye file 
% has the eye location for both eyes.
for d_i=1:length(days)  % Iterate through all of the specified days
    dayStr = num2str(days(d_i));
    % Prepend zeros if only 1 or 2 digits
    prepDayStr = dayStr;
    if (length(dayStr) == 1)
        prepDayStr = ['00' dayStr];
    elseif (length(dayStr) == 2)
        prepDayStr = ['0' dayStr];
    end
    
    if (~isempty(sessions))
        replaysFileList = dir([replaysFolder mouseName '-D' dayStr '-*-S' num2str(sessions(d_i)) '*']); %
    else
        replaysFileList = dir([replaysFolder mouseName '-D' dayStr '*']);
    end

    % If no replays found, print error and move on to next day
    if (isempty(replaysFileList))
        error(['Could not find replays for day = ' dayStr '. Continuing to next day.']);
        continue;
    end

    % Get the replayFileNames and sort them in trial order
    s = struct2cell(replaysFileList);
    replaysFileNames = natsortfiles(s(1,:));

    % Extract the scenario name from the replay filename, which will be used to open the correct actions file, though thi is probably not necessary
    expr = [mouseName '-D' dayStr '-([^-]+)-S([^-]+)-'];
    tokens = regexpi(replaysFileList(1).name, expr, 'tokens');
    scenarioName = tokens{1}{1};
    sessionNum = tokens{1}{2};

    % Open the actions file for this mouse on this day, whose number of lines will match the number of 
    % replay files for that day.  
    % We use the actions file to cleanup the replays plot, by extracting the
    % (1) Field restriction, nasal and temporal fields
    % (2) Associated location of the target - is it on the left or the right of center, 
    %     so we can invert the sign of the restriction
    % (3) Whether the trial was a success (2 sec wait), or failure (4 sec wait), 
    %     so we can truncate the trial plot appropriately at the end
    actionsFileName = [actionsFolder mouseName '-D' dayStr '-' scenarioName '-S' sessionNum '_actions.txt'];
    actionsFileID = fopen(actionsFileName);
    if (actionsFileID ~= -1)  % File was opened properly
        firstLine = fgets(actionsFileID); % Throw out the first line, as it is a column header
        actRecs = textscan(actionsFileID, actLineFormat); 
    else
        error(['Actions file ' actionsFileName 'could not be opened, so ending.']);
    end
    fclose(actionsFileID);  % If you forget to do this, then files no longer open and Matlab acts unpredictably

    % If writing new actions file with censored actions, open the file here for writing
    if (outputNewActionsFile)
        if (~interactive)
            newActionsFileName = [actionsFileName(1:end-4) '_censored.txt'];
            newActionsFileID = fopen(newActionsFileName, 'w');
            fprintf(newActionsFileID, firstLine);
        else
            % error('Cannot output new actions file and have interactive mode.  Pick one or the other.');
        end
    end
    
    % If using eye tracking to find center of gaze, load the relevant variables generated from analyzePupils.m
    if (~isempty(whichEye))
        load([eyevideosFolder mouseName '_' prepDayStr '_trk_an.mat'], 'azimDeg', 'trialStartFrames', 'trialEndFrames');
    end
    
    if (isempty(trials))
        trialsToDo = 1:length(actRecs{1});
    else
        lastTrial = trials(1);  % Assume only one trial, unless 2 numbers specified
        if (length(trials) > 1)
            if (trials(2) <= 0)
                lastTrial = length(actRecs{1});
            end
        end
        trialsToDo = trials(1):lastTrial;
    end
    
    % Before iterating through all trials, remove those that don't match the inclusion criteria, which includes
    % 1) StimLoc matches that which is specified, if any
    % 2) Whether corrections should be included or no
    % 3) Excluding catch trials
    % 4) Excluding extinction trials
    newTrialsToDo = [];
    for t_i=1:length(trialsToDo)
        stimLocX = getStimLoc(actRecs, trialsToDo(t_i));
        if (isempty(stimLocsToAnalyze) || ~isempty(find(stimLocsToAnalyze == stimLocX, 1)))
            % Exclude this trial if it is a correction trial and we are excluding correction trials
            if (includeCorrectionTrials || (~includeCorrectionTrials && ~getCorrection(actRecs, trialsToDo(t_i))))
                if (includeCatchTrials || (~includeCatchTrials && ~getCatch(actRecs, trialsToDo(t_i))))
                    if (includeExtinctionTrials || (~includeExtinctionTrials && ~getExtinction(actRecs, trialsToDo(t_i))))
                        newTrialsToDo = [newTrialsToDo trialsToDo(t_i)];
                    end
                end                       
            end
        end
    end
    trialsToDo = newTrialsToDo;

    % Iterate through each trial, drawing the angular bounds of the target
    trialIdx = 1;
    while trialIdx <= length(trialsToDo)       
        % First, find the stimulus location of this trial, and if the user specified which stimLocs to analyze,
        % include or skip as appropriate.
        stimLocX = getStimLoc(actRecs, trialsToDo(trialIdx));
        
        % Second, need to determine if this was a correct or incorrect trial
        actLocX = getActionLoc(actRecs, trialsToDo(trialIdx));
                
        % Third, find the nasal and temporal restrictions for this trial
        nasalBound = actRecs{8}(trialsToDo(trialIdx));
        temporalBound = actRecs{9}(trialsToDo(trialIdx));

        extinct = getExtinction(actRecs, trialsToDo(trialIdx));
        
        % Now, just plot based on the replay file
        % Ex line: 20000,0.74,20000;0;-51,-8;,
        replaysFileID = fopen([replaysFolder replaysFileNames{trialsToDo(trialIdx)}]);
        if (replaysFileID ~= -1)
            repRecs = textscan(replaysFileID, '%f %f %f %f %f %f %f %f', 'Delimiter', {';', ','}); 
            fclose(replaysFileID); % done with it.
            % Sometimes, if a game is canceled before starting, there might be a blank replay file.  This handles that, but somehow only some of the time.
            if (isempty(repRecs{1}))
                continue;
            end
            xStart = 1 + extraFramesAtStart;
            % Truncate the end of the replay file, since the screen is not displayed after the mouse makes a decision
            % Except if it is the last trial!
            lastTrial = trialsToDo(trialIdx) == trialsToDo(end);
            if (~lastTrial)
                if (stimLocX == actLocX)
                    xEnd = length(repRecs{1}) - successDelay * fps;
                else
                    xEnd = length(repRecs{1}) - failureDelay * fps;
                end
            else
                xEnd = length(repRecs{1});
            end
            xEnd = xEnd - extraFramesAtEnd;
            x = (xStart:xEnd)/fps;  % so units are in time, not frames
            targetLeftBound = repRecs{5};
            targetLeftBound = targetLeftBound(xStart:xEnd);
            targetRightBound = repRecs{6};
            targetRightBound = targetRightBound(xStart:xEnd);
            
            % First, many trials Unity reports the wrong location of the edge of the target, so need to clean
            % up to smoothen the plots.
            if (denoiseBallMovement)
                % For some unknown reason, on rig 1 (as Uranus D96-105 can testify), the near targets
                % are registered as -91,91 at the beginning of the trial.  So fix this first by
                % finding the first NON -91,91 value, and overwriting all early values with this value.
                if (targetLeftBound(1) == -91 && targetRightBound(1) == 91)
                    firstRealLeftIdx = find(targetLeftBound ~= -91,1);
                    firstRealRightIdx = find(targetRightBound ~= 91,1);
                    if (isempty(firstRealRightIdx) || (~isempty(firstRealLeftIdx) && firstRealLeftIdx < firstRealRightIdx))
                        targetLeftBound(1:firstRealLeftIdx-1) = targetLeftBound(firstRealLeftIdx);
                    else
                        targetRightBound(1:firstRealRightIdx-1) = targetRightBound(firstRealRightIdx);
                    end
                end
                targetLeftBound = denoiseBounds(targetLeftBound, maxAllowedJump);
                targetRightBound = denoiseBounds(targetRightBound, maxAllowedJump);
            end
                        
            % Second, adjust target bounds for when the target is not displayed on any monitor, so it isn't plotted
            relevantIdx = targetLeftBound == -91 & targetRightBound == 91;
            % Just needs to be the same value; Inf does not work
            targetLeftBound(relevantIdx) = nasalBound;
            targetRightBound(relevantIdx) = nasalBound; 
            
            % Third, account for field restriction to set the ACTUAL VISIBLE bounds of the target
            if (useFieldRestriction)
                if (stimLocX < stimCenter)
                    targetLeftBound(targetLeftBound < -temporalBound) = -temporalBound;
                    targetLeftBound(targetLeftBound > -nasalBound) = -nasalBound;
                    targetRightBound(targetRightBound > -nasalBound) = -nasalBound;
                    targetRightBound(targetRightBound < -temporalBound) = -temporalBound;
                elseif (stimLocX > stimCenter) % if stimLocX is center, don't restrict, just like in 3-choice
                    targetLeftBound(targetLeftBound < nasalBound) = nasalBound;
                    targetLeftBound(targetLeftBound > temporalBound) = temporalBound;
                    targetRightBound(targetRightBound > temporalBound) = temporalBound;
                    targetRightBound(targetRightBound < nasalBound) = nasalBound;
                end
            end
            
            % Fourth and finally, adjust the 0 point to be the center of the field of view without assuming
            % the mouse is just staring straight ahead and not moving her eyes.  Use eye tracking data to do this correction.
            % If both bounds were off-screen, since it was corrected to the same value, shifting it won't alter
            % the display in an irreparable way.
            if (~isempty(whichEye))
                if (whichEye == 'L')
                    whichEyeIdx = 1;
                elseif (whichEye == 'R')
                    whichEyeIdx = 2;
                else
                    error('Invalid eye specified');
                end
                eyeShift = azimDeg(trialStartFrames(trialsToDo(trialIdx)):trialEndFrames(trialsToDo(trialIdx)), whichEyeIdx);
                % Deal with NaNs in eye tracking (e.g. recorded when mouse blinks or pupil is occluded by a whisker or foam piece)
                % For now 0 - could interpolate in the future, but this is easier now.
                % Setting to 0 is OK because I want a definitive eye location to know when to censor a trial.
                % Right now even a single frame that brings the target into the good field will cause a trial to be censored,
                % so there won't be any benefit from interpolating.  So leave as setting to 0.  Note that now that we 
                % are also tracking corneal reflections, if either the corneal reflection center or pupil center cannot
                % be found, the eye location value will be NaN, and so set to 0 here.
                eyeShift(isnan(eyeShift)) = 0;
                frameCntDiff = length(eyeShift) - length(targetLeftBound);
                if (frameCntDiff > 0)
                    eyeShift = eyeShift(1:length(targetLeftBound));
                elseif (frameCntDiff < 0)
                    eyeShift = [eyeShift' zeros(1,-frameCntDiff)]';
                end
                % invert eye shift, as an eye movement to the right moves the object to the left relative to the center of gaze
                targetLeftBound = targetLeftBound - eyeShift;
                targetRightBound = targetRightBound - eyeShift;
            end

            left = -1;  % bool which says whether stim is on the left (1) or right (0) or center (-1)
            if (stimLocX == -1)  % Catch trial
                t = 'Catch';
                shade = shadingColorCenter;
                left = -2;
            elseif (stimLocX == stimLeftNear)
                t = 'Near Left';
                shade = shadingColorLeft;
                left = 1;
            elseif (stimLocX == stimLeftFar)
                t = 'Far Left';
                shade = shadingColorLeftFar;
                left = 1;
            elseif (stimLocX == stimRightNear)
                t = 'Near Right';
                shade = shadingColorRight;
                left = 0;
            elseif (stimLocX == stimRightFar)
                t = 'Far Right';
                shade = shadingColorRightFar;
                left = 0;
            elseif (stimLocX < stimCenter)
                t = 'Left';
                shade = shadingColorLeft;
                left = 1;
            elseif (stimLocX > stimCenter)
                t = 'Right';
                shade = shadingColorRight;
                left = 0;
            elseif (stimLocX == stimCenter)
                t = 'Center';
                shade = shadingColorCenter;
                left = -1;
            end
            
            if (actLocX == stimLeftNear)
                act = 'Near Left';
            elseif (actLocX == stimLeftFar)
                act = 'Far Left';
            elseif (actLocX == stimRightNear)
                act = 'Near Right';
            elseif (actLocX == stimRightFar)
                act = 'Far Right';
            elseif (actLocX < stimCenter)
                act = 'Left';
            elseif (actLocX > stimCenter)
                act = 'Right';
            elseif (actLocX == stimCenter)
                act = 'Center';
            end
            
            % If fractionOfRun is specified, only look over that interval of the run to determine the extrema
            if (length(fractionOfRun) == 1)  % for backwards compatibility with old threshold specification
                maxFrame = floor(fractionOfRun*length(targetLeftBound));
            else  % specified the exact frame for this trial, so use that
                maxFrame = fractionOfRun(trialsToDo(trialIdx));
            end
            if (left == 1 || left == 0)
                if (left == 1)
                    if (maxFrame > length(targetRightBound))
                        maxFrame = length(targetRightBound);
                    end
                    limitedRightBound = targetRightBound(1:maxFrame);
                    extreme = max(limitedRightBound(targetLeftBound(1:maxFrame) ~= limitedRightBound));
                    nasalExtremaL(end+1) = extreme;
                    m = 'L';
                    if (extinct)
                        m = 'LO';
                    end
                    extremeFrame = find(targetRightBound(targetLeftBound ~= targetRightBound) == extreme,1);
                elseif (left == 0)
                    if (maxFrame > length(targetLeftBound))
                        maxFrame = length(targetLeftBound);
                    end
                    limitedLeftBound = targetLeftBound(1:maxFrame);
                    extreme = min(limitedLeftBound(limitedLeftBound ~= targetRightBound(1:maxFrame)));
                    if (trialIdx == 155)
                        a=0;
                    end
                    nasalExtremaR(end+1) = extreme;
                    m = 'R';
                    if (extinct)
                        m = 'RO';
                    end
                    extremeFrame = find(targetLeftBound(targetLeftBound ~= targetRightBound) == extreme,1);
                end
                if (isempty(extreme))  % This happened on Uranus D96 T17 - I guess the mouse looked away from the target for the whole time.  Need to investigate.
                    error('Something is likely wrong with the ball tracking replay data.  No extreme found.');
                    if (left == 1)
                        totalTrialsNotAnalyzed(1) = totalTrialsNotAnalyzed(1) + 1;
                    elseif (left == 0)
                        totalTrialsNotAnalyzed(2) = totalTrialsNotAnalyzed(2) + 1;
                    end
                    if (verbose)
                        disp(['Excluded trial#' num2str(trialsToDo(trialIdx)) ' as no extreme found']);
                    end
                    trialIdx = trialIdx + 1;
                    continue;
                end
                idx = find(buckets >= extreme, 1);
                if (stimLocX == actLocX)
                    if (left == 1)
                        numCorrectL(idx) = numCorrectL(idx) + 1;
                    elseif (left == 0)
                        numCorrectR(idx) = numCorrectR(idx) + 1;
                    end
                else
                    if (left == 1)
                        numIncorrectL(idx) = numIncorrectL(idx) + 1;
                    elseif (left == 0)
                        numIncorrectR(idx) = numIncorrectR(idx) + 1;
                    end
                end
                if (verbose)
                    disp(['T' num2str(trialsToDo(trialIdx)) ...
                          ' - ' m ' - F' num2str(extremeFrame) ':' ...
                          num2str(extreme)]);
                end
            else % Target is centered or this is a catch trial, so no extrema and keep
                if (left == -1)
                    if (verbose)
                        disp(['T' num2str(trialsToDo(trialIdx)) ' - C']);
                    end
                elseif (left == -2)
                    if (verbose)
                        disp(['T' num2str(trialsToDo(trialIdx)) ' - Ca']);
                    end
                end
            end
            
            fr = '';
            if (~isempty(whichEye))
                fr = ['fr=' num2str(trialStartFrames(trialsToDo(trialIdx)))];
            end
                
            if (~interactive)
                if (outputNewActionsFile && newActionsFileID ~= -1)
                    % INCLUSION CRITERIA
                    if (isempty(targetAzimLimit) || left == -1 || left == -2 || ...
                            (left == 1 && nasalExtremaL(end) < targetAzimLimit(1)) || ...
                            (left == 1 && nasalExtremaL(end) >= targetAzimLimit(1) && censorOnlyIfCorrect && stimLocX ~= actLocX) || ...
                            (left == 0 && nasalExtremaR(end) > targetAzimLimit(2)) || ...
                            (left == 0 && nasalExtremaR(end) <= targetAzimLimit(2) && censorOnlyIfCorrect && stimLocX ~= actLocX))
                        if (~writeOutOnlyIfCensored)
                            tca = cellfun(@(v) v(trialsToDo(trialIdx)), actRecs, 'UniformOutput', 0);
                            tca2 = cell(size(tca));
                            for m=1:length(tca)
                                if iscell(tca{m})
                                    tca2{m} = tca{m}{1};
                                elseif isnumeric(tca{m})
                                    tca2{m} = tca{m};
                                end
                            end
                            fprintf(newActionsFileID, [actLineFormat '\n'], tca2{:});
                            if (left == 1)
                                if (extinct == 0)
                                    totalTrialsAnalyzed(1) = totalTrialsAnalyzed(1) + 1;
                                else
                                    totalExtinctTrialsAnalyzed(1) = totalExtinctTrialsAnalyzed(1) + 1;
                                end
                            elseif (left == 0)
                                if (extinct == 0)
                                    totalTrialsAnalyzed(2) = totalTrialsAnalyzed(2) + 1;
                                else
                                    totalExtinctTrialsAnalyzed(2) = totalExtinctTrialsAnalyzed(2) + 1;
                                end
                            end
                        else
                            if (left == 1)
                                if (extinct == 0)
                                    totalTrialsNotAnalyzed(1) = totalTrialsNotAnalyzed(1) + 1;
                                else
                                    totalExtinctTrialsNotAnalyzed(2) = totalExtinctTrialsNotAnalyzed(2) + 1;
                                end
                            elseif (left == 0)
                                if (extinct == 0)
                                    totalTrialsNotAnalyzed(2) = totalTrialsNotAnalyzed(2) + 1;
                                else
                                    totalExtinctTrialsNotAnalyzed(2) = totalExtinctTrialsNotAnalyzed(2) + 1;
                                end
                            end
                            if (verbose)
                                disp(['Excluded valid trial#' num2str(trialsToDo(trialIdx)) ' as within allowed target azimuth']);
                            end
                        end
                    else
                        if (writeOutOnlyIfCensored)
                            tca = cellfun(@(v) v(trialsToDo(trialIdx)), actRecs, 'UniformOutput', 0);
                            tca2 = cell(size(tca));
                            for m=1:length(tca)
                                if iscell(tca{m})
                                    tca2{m} = tca{m}{1};
                                elseif isnumeric(tca{m})
                                    tca2{m} = tca{m};
                                end
                            end
                            fprintf(newActionsFileID, [actLineFormat '\n'], tca2{:});
                            if (left == 1)
                                if (extinct == 0)
                                    totalTrialsAnalyzed(1) = totalTrialsAnalyzed(1) + 1;
                                else
                                    totalExtinctTrialsAnalyzed(1) = totalExtinctTrialsAnalyzed(1) + 1;
                                end
                            elseif (left == 0)
                                if (extinct == 0)
                                    totalTrialsAnalyzed(2) = totalTrialsAnalyzed(2) + 1;
                                else
                                    totalExtinctTrialsAnalyzed(2) = totalExtinctTrialsAnalyzed(2) + 1;
                                end
                            end
                        else
                            if (left == 1)
                                if (extinct == 0)
                                    totalTrialsNotAnalyzed(1) = totalTrialsNotAnalyzed(1) + 1;
                                else
                                    totalExtinctTrialsNotAnalyzed(2) = totalExtinctTrialsNotAnalyzed(2) + 1;
                                end
                            elseif (left == 0)
                                if (extinct == 0)
                                    totalTrialsNotAnalyzed(2) = totalTrialsNotAnalyzed(2) + 1;
                                else
                                    totalExtinctTrialsNotAnalyzed(2) = totalExtinctTrialsNotAnalyzed(2) + 1;
                                end
                            end
                            if (verbose)
                                disp(['Censored trial#' num2str(trialsToDo(trialIdx)) ' as beyond allowed target azimuth']);
                            end
                        end
                    end
                end
                trialIdx = trialIdx + 1;
            else
                f1 = figure; 
                set(f1, 'Position', [68+530 590 800 400])
                subplot(1,2,2);
                hold on;
                set(gcf,'color','w');
                set(gca, 'Layer', 'top');
                set(gca, 'Position', [0.5 0.13 0.48 0.7]); 
                %set(f1, 'MenuBar', 'none');
                %set(f1, 'ToolBar', 'none');

                % Plot the initial period where the mouse is frozen at the start
                patch([-90 90 90 -90], [0 0 immobilePeriod immobilePeriod], grayShade, 'LineStyle', 'None');
                % Plot the actual size of the target
                patch([targetLeftBound' flip(targetRightBound')], [x flip(x)], shade, 'LineStyle', 'None');
                % Plot black border, to accentuate the shaded region.  But to do this, replace equal values with NaNs, 
                % so we don't have border lines without interior shading
                matchingBoundsIdx = targetLeftBound == targetRightBound;
                targetLeftBound(matchingBoundsIdx) = NaN;
                targetRightBound(matchingBoundsIdx) = NaN;
                plot(targetLeftBound, x, 'k-');
                plot(targetRightBound, x, 'k-');
                % Plot top boundary when trial ends explicitly, as not visible on a white figure background otherwise
                plot([-90 90], [x(end) x(end)], 'k');
                % Plot 0 degree  dotted line
                plot([0 0], [x(1) x(end)], 'k--');
                xlim([-90 90]);
                ylim([0 x(end)]);
                xticks(-90:15:90);
                set(gca, 'XTickLabel', []);
                xticklabels({'-90', '', '-60', '', '-30', '', '0', '', '30', '', '60', '', '90'});
                xlabel('Target Azimuth (deg)');
                ylabel('Time (s)');
                c = colorbar;
                colormap(jet);
                c.Location = 'westoutside';
                c.Box = 'off';
                c.TickLabels = {};
                c.Ticks = [];

                if (useFieldRestriction)
                    r = 'r';
                else
                    r = 'ur';
                end
                title([mouseName '-D' dayStr ': T' num2str(trialsToDo(trialIdx))  ', ' fr ', ' r]);

                % Also plot the animal's trajectory on that trial
                subplot(1,2,1);
                set(gca, 'Position', [0.04 0.05 0.44 0.9]);
                analyzeTraj(mouseName, days(d_i), [], trialsToDo(trialIdx), trialTypeStrArr, 1, 0, 8, 1, lastTrial, 1, 0, 1, 'b');

                % Logic to only keep open the figs that you hit 'k' for 'keep' on
                closeFig = 1;
                while true
                    w = waitforbuttonpress; 
                    switch w 
                        case 1 % (keyboard press) 
                          key = get(gcf,'currentcharacter'); 
                          switch key
                              case 'k'
                                  closeFig = 0;
                              case 13 % 13 is the return key 
                                  trialIdx = trialIdx+1;
                                  break
                              case 29 % RIGHT arrow key
                                  trialIdx = trialIdx+1;
                                  break  % go forward one trial
                              case 28 % LEFT arrow key
                                  if (trialIdx ~= 1)
                                      trialIdx = trialIdx-1; % go back one trial
                                  end
                                  break
                              case 27 % ESC key
                                  trialIdx = length(trialsToDo)+1;
                                  break
                              otherwise 
                                  % Wait for a different command. 
                          end
                     end
                end
                if (closeFig)
                    close(f1);
                end
            end
        else
            error(['Replays file ' replaysFileNames(1) 'could not be opened, so ending.']);
        end
    end
end

accuracyPerExtremaL = numCorrectL ./ (numCorrectL + numIncorrectL);
accuracyPerExtremaR = numCorrectR ./ (numCorrectR + numIncorrectR);

pctAnalyzed = totalTrialsAnalyzed ./ (totalTrialsAnalyzed + totalTrialsNotAnalyzed);
pctExtinctAnalyzed = totalExtinctTrialsAnalyzed ./ (totalExtinctTrialsAnalyzed + totalExtinctTrialsNotAnalyzed);

figure;
hold on;
yyaxis left;
blue = [2 87 194]/255;
red = [226 50 50]/255;
histogram(nasalExtremaL, leftExtreme:bucketSize:rightExtreme, 'Normalization', 'probability', 'FaceColor', blue);
histogram(nasalExtremaR, leftExtreme:bucketSize:rightExtreme, 'Normalization', 'probability', 'FaceColor', red);
ylabel('fraction (bars)', 'Color', 'black');
set(gca, 'YColor', 'k');

green = [0.4660 0.6740 0.1880];
yyaxis right;
scatter(buckets(2:end-1) - bucketSize/2, accuracyPerExtremaL(2:end-1),40, ...
        'MarkerEdgeColor', blue, 'MarkerFaceColor', blue);
scatter(buckets(2:end-1) - bucketSize/2, accuracyPerExtremaR(2:end-1),40, ...
        'MarkerEdgeColor', red, 'MarkerFaceColor', red);
xlabel('most nasal edge of target (degrees)');
ylabel('accuracy (dots)', 'Color', 'black');
ylim([-0.1 1.1]);
set(gca, 'YColor', 'k');
% Plot 0 degree  dotted line
plot([0 0], ylim, 'k--');
title([mouseName ', ' whichEye ' eye, D' num2str(days) ': n_L=' num2str(length(nasalExtremaL)) ' (' num2str(pctAnalyzed(1)*100,2) ...
        '% kept), n_R=' num2str(length(nasalExtremaR)) ' (' num2str(pctAnalyzed(2)*100,2) '% kept), c=' num2str(fractionOfRun)]);

if (~interactive && outputNewActionsFile && newActionsFileID ~= -1)
    fclose(newActionsFileID);
    modifier = 'uncensored';
    if (writeOutOnlyIfCensored)
        modifier = 'censored';
    end
    disp(['Total ' modifier ' trials written to file = ' num2str(sum(totalTrialsAnalyzed))]);
    disp(['L trials kept = ' num2str(pctAnalyzed(1)*100,2) '% (' num2str(totalTrialsAnalyzed(1)) '/' num2str(totalTrialsAnalyzed(1) + totalTrialsNotAnalyzed(1)) ')']);
    disp(['L extinction trials kept = ' num2str(pctExtinctAnalyzed(1)*100,2) '% (' num2str(totalExtinctTrialsAnalyzed(1)) '/' num2str(totalExtinctTrialsAnalyzed(1) + totalExtinctTrialsNotAnalyzed(1)) ')']);
    disp(['R trials kept = ' num2str(pctAnalyzed(2)*100,2) '% (' num2str(totalTrialsAnalyzed(2)) '/' num2str(totalTrialsAnalyzed(2) + totalTrialsNotAnalyzed(2)) ')']);
    disp(['R extinction trials kept = ' num2str(pctExtinctAnalyzed(2)*100,2) '% (' num2str(totalExtinctTrialsAnalyzed(2)) '/' num2str(totalExtinctTrialsAnalyzed(2) + totalExtinctTrialsNotAnalyzed(2)) ')']);
    disp(['Mean of nasal extrema histograms: L = ' num2str(mean(nasalExtremaL)) ', R = ' num2str(mean(nasalExtremaR))]);
end

end