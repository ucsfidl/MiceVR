function plotTargetAzimLoc(mouseName, days, sessions, trials, trialTypeStrArr, denoise, useFieldRestriction, useEyeTracking, whichEye)
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

%%% CHANGE THESE VARS FOR YOUR SETUP PRIOR TO RUNNING %%%
scenariosFolder = 'C:\Users\nikhil\Documents\GitHub\MiceVR\scenarios\';
actionsFolder = 'C:\Users\nikhil\UCB\data-VR\';
replaysFolder = 'C:\Users\nikhil\UCB\data-replays\';
eyevideosFolder = 'C:\Users\nikhil\UCB\data-eyevideos\';

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

maxAllowedJump = 10;  % max allowed change in the target's azimuthal location, for removing noise and smoothening

% Error out if number of sessions is non-zero and does not match number of days.
if (~isempty(sessions) && length(days) ~= length(sessions))
    error('Number of sessions is non-zero and does not match number of days. It should.')
end
% 
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
        disp(['Could not find replays for day = ' dayStr '. Continuing to next day.']);
        continue; 
    end

    % Get the replayFileNames and sort them in trial order
    s = struct2cell(replaysFileList);
    replaysFileNames = natsortfiles(s(1,:));

    % Extract the scenario name from the replay filename, which will be used to open the correct actions file, though thi is probably not necessary
    expr = [mouseName '-D' dayStr '-([^-]+)-S([^-]+)-'];
    tokens = regexp(replaysFileList(1).name, expr, 'tokens');
    scenarioName = tokens{1}{1};
    sessionNum = tokens{1}{2};

    % Open the actions file for this mouse on this day, whose number of lines will match the number of 
    % replay files for that day.  
    % We use the actions file to cleanup the replays plot, by extracting the
    % (1) Field restriction, nasal and temporal fields
    % (2) Associated location of the target - is it on the left or the right of center, 
    %     so we can invert the sign of the restriction
    % (3) Whether the trial was a success (2 sec wait), or failure (4 sec wait), 
    %     so we can truncate truncate the plot at the end
    actionsFileName = [actionsFolder mouseName '-D' dayStr '-' scenarioName '-S' sessionNum '_actions.txt'];
    actionsFileID = fopen(actionsFileName);
    if (actionsFileID ~= -1)  % File was opened properly
        fgetl(actionsFileID); % Throw out the first line, as it is a column header
        actRecs = textscan(actionsFileID, '%s %s %d %s %s %d %d %d %d %d %d %s %d %d %f %d %d %d %d %d %d'); 
    else
        error(['Actions file ' actionsFileName 'could not be opened, so ending.']);
    end
    fclose(actionsFileID);  % If you forget to do this, then files no longer open and Matlab acts unpredictably

    % If using eye tracking to find center of gaze, load the relevant variables generated from analyzePupils.m
    if (useEyeTracking)
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
    

    % Iterate through each trial, drawing the angular bounds of the target
    trialIdx = 1;
    while trialIdx <= length(trialsToDo)
        % First, need to determine if this was a correct or incorrect trial
        % actRecs{5} is the target location, actRecs{12} is the turn location
        stimLocX = getStimLocFromActions(actRecs, trialsToDo(trialIdx));
        actLocX = getActionLocFromActions(actRecs, trialsToDo(trialIdx));
        
        % Second, find the nasal and temporal restrictions for this trial
        nasalBound = actRecs{8}(trialsToDo(trialIdx));
        temporalBound = actRecs{9}(trialsToDo(trialIdx));

        % Now, just plot based on the replay file
        % Ex line: 20000,0.74,20000;0;-51,-8;,
        replaysFileID = fopen([replaysFolder replaysFileNames{trialsToDo(trialIdx)}]);
        if (replaysFileID ~= -1)
            repRecs = textscan(replaysFileID, '%f %f %f %f %f %f %f %f', 'Delimiter', {';', ','}); 
            xStart = 1 + extraFramesAtStart;
            % Truncate the end of the replay file, since the screen is not displayed after the mouse makes a decision
            if (stimLocX == actLocX)
                xEnd = length(repRecs{1}) - successDelay * fps;
            else
                xEnd = length(repRecs{1}) - failureDelay * fps;
            end
            xEnd = xEnd - extraFramesAtEnd;
            x = (xStart:xEnd)/fps;  % so units are in time, not frames
            targetLeftBound = repRecs{5};
            targetLeftBound = targetLeftBound(xStart:xEnd);
            targetRightBound = repRecs{6};
            targetRightBound = targetRightBound(xStart:xEnd);
            
            % First, many trials Unity reports the wrong location of the edge of the target, so need to clean
            % up to smoothen the plots.
            if (denoise)
                targetLeftBound = denoiseBounds(targetLeftBound, maxAllowedJump);
                targetRightBound = denoiseBounds(targetRightBound, maxAllowedJump);
            end
                        
            % Second, adjust target bounds for when the target is not displayed on any monitor, so it isn't plotted
            targetLeftBoundPrev = targetLeftBound;
            targetLeftBound(targetLeftBound == -91 & targetRightBound == 91) = nasalBound;
            targetRightBound(targetLeftBoundPrev == -91 & targetRightBound == 91) = nasalBound; % Just needs to be the same value; Inf does not work

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
            % the mouse is just staring straight ahead.  Use eye tracking data to do this correction.
            % If both bounds were off-screen, since it was corrected to the same value, shifting it won't alter
            % the display in an irreparable way.
            if (useEyeTracking)
                eyeShift = azimDeg(trialStartFrames(trialsToDo(trialIdx)):trialEndFrames(trialsToDo(trialIdx)), whichEye);
                % Deal with NaNs in eye tracking (e.g. recorded when mouse blinks)
                % For now 0 - could interpolate in the future, but this is easier now
                eyeShift(isnan(eyeShift)) = 0;
                % Compare with using the R eye - any substantial difference?
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
            if (stimLocX == stimLeftNear)
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
            
            if (left == 1)
                disp(['T' num2str(trialsToDo(trialIdx)) ...
                      '-F' num2str(find(targetRightBound == max(targetRightBound))) ':' ...
                      num2str(max(targetRightBound))]);
            elseif (left == 0)
                disp(['T' num2str(trialsToDo(trialIdx)) ...
                      '-F' num2str(find(targetLeftBound == min(targetLeftBound))) ':' ...
                      num2str(min(targetLeftBound))]);
            end
            
            fr = ['fr=' num2str(trialStartFrames(trialsToDo(trialIdx)))];
            
            f1 = figure; hold on
            set(gcf,'color','w');
            set(gca, 'Layer', 'top');
            % Plot the initial period where the mouse is frozen at the start
            patch([-90 90 90 -90], [0 0 immobilePeriod immobilePeriod], grayShade, 'LineStyle', 'None');
            % Plot the actual size of the targe
            patch([targetLeftBound' flip(targetRightBound')],[x flip(x)], shade, 'LineStyle', 'None');
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
            
            if (useFieldRestriction)
                r = 'r';
            else
                r = 'ur';
            end
            title([mouseName '-D' dayStr ': T' num2str(trialsToDo(trialIdx))  ', ' fr ', ' r ', ' t '->' act]);

            fclose(replaysFileID); % done with it.

            % Also plot the animal's trajectory on that trial
            f2 = analyzeTraj(mouseName, days(d_i), [], trialsToDo(trialIdx), trialTypeStrArr, 1, 0, 8, 0.06);
            
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
                          case 27
                              trialIdx = length(trialsToDo)+1;
                              break
                          otherwise 
                              % Wait for a different command. 
                      end
                 end
            end
            if (closeFig)
                close(f1);
                close(f2);
            end
        else
            error(['Replays file ' replaysFileNames(1) 'could not be opened, so ending.']);
        end
    end

end

end