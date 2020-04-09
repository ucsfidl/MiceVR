function [cutoff, predictionAccuracy] = findEarliestTimepointOfDecision(mouseName, days, sessions, minAccuracy, ...
                                                                        poolingSize)
% This is a helper function to help identify what the cutoff should be for censoring a trial
% if the mouse moved the target into its good field before the cutoff timepoint.  The cutoff is what is outputted
% by this function, and it is the earliest timepoint in a normalized trial (range 0-100%) in which
% the mouse's decision on the trial can be predicted with a user-specified accuracy (e.g. 70%). 

% To determine this, the program will do a binary search between 0-100%, with a step size of 1%.

% The program will iterate through all replays and find the location of the mouse at t=50%. It will also
% log where the mouse decided to go for that trial.  Then, it will analyze this 2-d array to produce
% a 3-d array (x,z,4), where each position within 1 world point (or another bucket size) will be pooled and the mouse's
% ultimate decision will be logged.  

% If this results in an accuracy for all 4 target locations of greater or equal to the user-specified accuracy, 
% try this with a cutoff of 25%.  If the accuract was less than or equal to the user-specified accuracy,
% try again with a cutoff of 75%.  Do this kind of binary search until the smallest cutoff that satisfies the accuracy
% constraint is satisfied, and then return that.

% This analysis does not take into account gaze and thereby target position in the visual field, just mouse position in the world.

% Be sure to change these if the x location of the trees changes
stimLeftNear = 19973;
stimLeftFar = 19972;
stimRightNear = 20027;
stimRightFar = 20028;

colors = [1 1 1;   % white, for places on the winnerMask which are not used for predictions
          0.84 0.89 0.99;      % dull blue
          0.84 0.98 0.99;     % dull cyan
          1 0.87 0.71;     % dull orange
          0.9 0.9 0.69;   % dull yellow
          ];

actionsFolder = 'C:\Users\nikhi\UCB\data-actions\';
replaysFolder = 'C:\Users\nikhi\UCB\data-replays\';

actLineFormat = '%s\t%s\t%d\t%s\t%s\t%d\t%d\t%d\t%d\t%d\t%d\t%s\t%d\t%d\t%f\t%d\t%d\t%d\t%d\t%d\t%d';

fps = 60;
successDelay = 2; %sec
failureDelay = 4; %sec

totalTrialsAnalyzed = 0;

% Use user-provided argument instead
% poolingSize = 2;  % This is for pooling slightly different locations into 1 bucket, so accuracy stats can be calcuated.
                 % A pooling size of 1 means 1x1 (just rounding to the nearest integer), 2 means 2x2, 3 means 3x3, etc.

allDone = 0;

replaysFileList = [];
trialsPerDay = zeros(length(days), 1);
for d_i=1:length(days)  % Iterate through all of the specified days, collecting all relevant replays
    dayStr = num2str(days(d_i));
    if (~isempty(sessions))
        newList = dir([replaysFolder mouseName '-D' dayStr '-*-S' num2str(sessions(d_i)) '*']);
        replaysFileList = [replaysFileList; newList]; %
    else
        newList = dir([replaysFolder mouseName '-D' dayStr '*']);
        replaysFileList = [replaysFileList; newList];
    end
    trialsPerDay(d_i) = length(replaysFileList);
end

% If no replays found, print error and move on to next day
if (isempty(replaysFileList))
    error(['Could not find replays at least one of the days specified.']);
end

% Get the replayFileNames and sort them in trial order
s = struct2cell(replaysFileList);
replaysFileNames = natsortfiles(s(1,:));

% Extract the scenario name from the replay filename, which will be used to open the correct actions file
actRecs = [];
for d_i=1:length(days)  % Iterate through all of the specified days, collecting all relevant replays
    dayStr = num2str(days(d_i));
    expr = [mouseName '-D' dayStr '-([^-]+)-S([^-]+)-'];
    idx = 1;
    if (d_i > 1)
        idx = idx + trialsPerDay(d_i-1);
    end
    tokens = regexp(replaysFileList(idx).name, expr, 'tokens');
    scenarioName = tokens{1}{1};
    sessionNum = tokens{1}{2};

    % Open the actions file for this mouse on this day, whose number of lines will match the number of 
    % replay files for that day.  
    % We use the actions file to determine where the mouse decided to go on that trial.
    actionsFileName = [actionsFolder mouseName '-D' dayStr '-' scenarioName '-S' sessionNum '_actions.txt'];
    actionsFileID = fopen(actionsFileName);
    if (actionsFileID ~= -1)  % File was opened properly
        firstLine = fgets(actionsFileID); % Throw out the first line, as it is a column header
        if (length(actRecs) == 0)
            actRecs = textscan(actionsFileID, actLineFormat); 
        else
            tmpActRecs = textscan(actionsFileID, actLineFormat);
            for actCol=1:length(actRecs)
                actRecs{actCol} = [actRecs{actCol}; tmpActRecs{actCol}];
            end
        end
    else
        error(['Actions file ' actionsFileName 'could not be opened, so ending.']);
    end
    fclose(actionsFileID); 
end

numTrials = length(actRecs{1});
mouseLocToActLoc = zeros(numTrials, 3);

lastHighCutoff = 1;
lastLowCutoff = 0;
cutoff = 0.15;  % Starting value for binary search
while (1) % a while loop to iterate until earliest cutoff is found
    for currTrial = 1:numTrials
        % By default include correction trials, as those are useful info.  Consider excluding if don't get good results.
        actLocX = getActionLocFromActions(actRecs, currTrial);
        stimLocX = getStimLocFromActions(actRecs, currTrial);

        % Now find the currPercentile position
        replaysFileID = fopen([replaysFolder replaysFileNames{currTrial}]);
        if (replaysFileID ~= -1)
            repRecs = textscan(replaysFileID, '%f %f %f %f %f %f %f %f', 'Delimiter', {';', ','}); 
            fclose(replaysFileID); % done with it.
            % Sometimes, if a game is canceled before starting, there might be a blank replay file.  This handles that.
            if (isempty(repRecs{1}))
                disp('skipping empty replay file...');
                continue;
            end
            numFramesToExcludeAtEnd = successDelay * fps;
            if (actLocX ~= stimLocX)
                numFramesToExcludeAtEnd = failureDelay * fps;
            end

            trialDur = length(repRecs{1}) - numFramesToExcludeAtEnd;
            sampleFrame = round(cutoff * trialDur);
            mouseLocToActLoc(currTrial,1) = repRecs{3}(sampleFrame);  % z Pos
            mouseLocToActLoc(currTrial,2) = repRecs{1}(sampleFrame);  % x Pos
            mouseLocToActLoc(currTrial,3) = actLocX;
        end
    end

    minZVal = floor(min(mouseLocToActLoc(:,1)));
    maxZVal = ceil(max(mouseLocToActLoc(:,1)));
    zRange = round((maxZVal - minZVal) / poolingSize) + 1;
    minXVal = floor(min(mouseLocToActLoc(:,2)));
    maxXVal = ceil(max(mouseLocToActLoc(:,2))); 
    xRange = round((maxXVal - minXVal) / poolingSize) + 1;
    binnedLocs = zeros(zRange, xRange, 4);

    for idx=1:length(mouseLocToActLoc(:,1))
        zIdx = round((mouseLocToActLoc(idx,1) - minZVal) / poolingSize) + 1;
        xIdx = round((mouseLocToActLoc(idx,2) - minXVal) / poolingSize) + 1;
        if (mouseLocToActLoc(idx,3) == stimLeftNear)
            actIdx = 1;
        elseif (mouseLocToActLoc(idx,3) == stimLeftFar)
            actIdx = 2;
        elseif (mouseLocToActLoc(idx,3) == stimRightNear)
            actIdx = 3;
        else
            actIdx = 4;
        end
        if (idx == 82)
            %disp('in');
        end
        binnedLocs(zIdx,  xIdx, actIdx) = binnedLocs(zIdx,  xIdx, actIdx) + 1;
    end

    locAccuracy = zeros(size(binnedLocs));
    for idx=1:4
        locAccuracy(:,:,idx) = binnedLocs(:,:,idx) ./ sum(binnedLocs,3);
    end
    
    % append a NaN array to beginning of locAccuracy, just for the mask calculation
    tmpLocAccuracy = cat(3, nan(zRange, xRange), locAccuracy);
    
    % This array will keep an index of the winner at each location - that is, the highest probability actLoc at that xz location
    [mx winnerMask] = max(tmpLocAccuracy, [], 3);
    winnerMask = winnerMask - 1;
    
    predictionAccuracy = zeros(4,1);
    numerAll = 0;
    denomAll = 0;
    for idx=1:4
        indices = winnerMask == idx;
        currArray = binnedLocs(:,:,idx);
        numer = sum(currArray(indices));
        denom = 0;
        for idx2=1:4
            currArray2 = binnedLocs(:,:,idx2);
            denom = denom + sum(currArray2(indices));
        end
        predictionAccuracy(idx) = numer / denom;
        numerAll = numerAll + numer;
        denomAll = denomAll + denom;
    end
    
    if (sum(isnan(predictionAccuracy)) > 0 || numerAll / denomAll < minAccuracy)  % push decision point to later
        newCutoff = round(cutoff + (lastHighCutoff - cutoff) / 2, 2);
        if (newCutoff == cutoff)
            break;
        end
        lastLowCutoff = cutoff;
        cutoff = newCutoff;
    else % push decision point earlier
        newCutoff = round(cutoff - (cutoff - lastLowCutoff) / 2, 2);
        if (newCutoff == cutoff)
            break;
        end
        lastHighCutoff = cutoff;
        cutoff = newCutoff;
    end
end

figure;
colormap(colors);
imagesc(flipud(winnerMask));
%pcolor(winnerMask);  % the grid adds too much visual noise

end