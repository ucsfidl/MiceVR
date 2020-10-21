function [f, mX, mZ, xCI95, zCI95] = analyzeTraj(mouseName, days, sessions, trials, trialTypeStrArr, ...
                                                includeCorrectionTrials, drawOneFig, markSize, markAlpha, ...
                                                lastTrial, useSubPlot, hideStraightTarget, plotIndivTrials, ...
                                                avgTrajColor)
% SAMPLE USAGE
% // MANY Tracks on one plot
% > analyzeTraj('Mania', [120:124], [], [], ["R->R"], 0, 1, 4, 0.1, 0, 0)
% > analyzeTraj('Vixen', [43], [], [], ["L->L" "R->R" "C->C"], 0, 1, 1, 0.02, 0)
% // EACH TRIAL on a separate plot
% > analyzeTraj('Dragon', [182], [], [], [], 1, 0, 8, 0.06, 0)
%
% This function takes as input a mouse's name as well as the days and corresponding sessions that 
% should be analyzed.  It then looks in the replay directory (hard-coded - change if it is somewhere 
% else on your machine) to find the corresponding mouse position and heading files.  By reading the 
% replay filename, the scenario can be extracted so the map can be drawn.  This is done by reading the 
% scenario file in the scenarios directory (again, hard-coded, so change to match on your machine) to 
% extract the target and wall locations so that an appropriate map can be drawn.
%
% TODO: Option to plot the average path in addition to individual trial paths
%
% trialTypeStrArr specifies which trial types should be analyzed:
%   "*-*" - all targets, all actions
%   "L-*" - left target, all actions
%   "L-L" - left target, left action
%   "L-R" - left target, right action
%   "L-S" - left target, straight action
%   "R-*" - right target, all actions
%   "R-L" - right target, left action
%   "R-R" - right target, right action
%   "R-S" - right target, straight action
%   "S-*" - straight target, all actions
%   "S-L" - straight target, left action
%   "S-R" - straight target, right action
%   "S-S" - straight target, straight action
%   "LO-*"
%   "LO-L"
%   "LO-R"
%   "LO-S"
%   "RO-*"
%   "RO-L"
%   "RO-R"
%   "RO-S"
%   "Ca-*"
%   "Ca-L"
%   "Ca-R"
%   "Ca-S"

%   "LN->*"
%   "LN->LN"
%   "LN->RN"
%   "LN->LF"
%   "LN->RF"
%   "RN->*"
%   "RN->LN"
%   "RN->RN"
%   "RN->LF"
%   "RN->RF"
%   "LF->*"
%   "LF->LN"
%   "LF->RN"
%   "LF->LF"
%   "LF->RF"
%   "RF->*"
%   "RF->LN"
%   "RF->RN"
%   "RF->LF"
%   "RF->RF"
%   "Ca-*"
%   "Ca-LN"
%   "Ca-RN"
%   "Ca-LF"
%   "Ca-RF"
%   
%
% If trialTypeStrArr is specified (e.g. show only correct actions: ["L-L" "R-R" "S-S"],
% the program will also read the actions files for the corresponding
% day/session to determine which trial type it was and to only include
% those that map those asked for by the user.
%
% For each trajectory plot, a single trajectory is plotted as a rainbow line,
% with overlapping trajectories getting a bump in darkness.  The colors of the rainbow map to the 
% timeline of that specific trial. Finally, a mean trajectory for that trial
% type is also shown in a solid color as specified by a parameter.
%
% Alternatively, this program could generate a video file with videos of
% the trajectories.
%
% I need to think about whether and how to incorporate speed and heading in these plots.
% My instinct is to just ignore heading for now and just put arrows to
% indicate direction along the curve, ignoring speed as well.
% Alternatively, speed could be indicated by a thicker spot indicating a
% slower speed and a thinner spot indicating a faster speed.  Maybe I will
% get Tunlin's take on this as well.

%%% CHANGE THESE VARS FOR YOUR SETUP PRIOR TO RUNNING %%%
scenariosFolder = 'C:\Users\nikhi\Documents\GitHub\MiceVR\scenarios\';
actionsFolder = 'C:\Users\nikhi\UCB\data-actions\';
replaysFolder = 'C:\Users\nikhi\UCB\data-replays\';

actLineFormat = getActionLineFormat();

% The old log files analyzed trial types by location.  To analyze old log files (pre-Junish 2020), 
% use analyzeTraj_v1.m.
%
% The current method is to look at the targetIdx and TurnIdx columns for the target locations.
% For 3-choice, L=0, R=1, and S=2.
% For 4-choice, NL=0, NR=1, FL=2, FR=3.
% For 2-choice, NL=0, FL=1, NR=0, FR=1 (the worldNum distinguishes left(0) and right(1) worlds).
% Catch trials have a targetIdx of -1 across all worlds.

startX = 20000;
startZ = 20000;

leftIdx = 0;
rightIdx = 1;
straightIdx = 2;

leftNearIdx = 0;
rightNearIdx = 1;
leftFarIdx = 2;
rightFarIdx = 3;

nearLeftIdx2C = 0;
farLeftIdx2C = 1;
nearRightIdx2C = 0;
farRightIdx2C = 1;

wallColor = [0.85 0.85 0.85];
wallWidth = 20;

shadingColorLeft = [0.84 0.89 0.99];  % dull blue
shadingColorLeftFar = [0.84 0.98 0.99]; % dull cyan
shadingColorRight = [1 0.87 0.71]; % dull orange
shadingColorRightFar = [0.9 0.9 0.69];  % dull yellow
shadingColorStraight = [0.85 .99 0.84]; % dull green

correctDelay = 2;
incorrectDelay = 4;
fps = 60;

% Error out if number of sessions is non-zero and does not match number of days.
if (~isempty(sessions) && length(days) ~= length(sessions))
    error('Number of sessions is non-zero and does not match number of days. It should.')
end

% Used to count the separate figures made and lay them out on my laptop screen in a reasonable, 
% though still unwieldy, manner
figN = 0;  

if(isempty(trialTypeStrArr))  % If no string specified, initialize to analyze all data
    trialTypeStrArr = "*-*";
end

numTrialsTotal = 0;

% First, iterate through all of the different trialTypes specified, as generally will make one plot 
% per trialType if drawOneFig is specified.
for tt_i=1:length(trialTypeStrArr)
    % Local vars used for this figure's plot title
    dayStr = '';
    numReplaysInFig = 0;
    daysPlotted = 0;
    if (includeCorrectionTrials)
        corrTxt = 'NC & Co';
    else
        corrTxt = 'NC';
    end

    % Generally will be drawing one figure, so init a figure for drawing
    if (drawOneFig)
        figN = figN+1;
        f = initTrajFig(figN, useSubPlot);
    end
    
    totalTrials = 0;
    trajX = {};  % used to store each replay so I can plot the average
    trajZ = {};  % used to store each replay so I can plot the average
    minTrajLength = Inf;
    for d_i=1:length(days)  % Iterate through all of the specified days
        dayNum = num2str(days(d_i));
        if (~isempty(sessions))
            replaysFileList = dir([replaysFolder mouseName '-D' dayNum '-*-S' num2str(sessions(d_i)) '*']);
        else
            replaysFileList = dir([replaysFolder mouseName '-D' dayNum '*']);
        end

        % If no replays found, print error and move on to next day
        if (isempty(replaysFileList))
            disp(['Could not find replays for day = ' dayNum '. Continuing to next day.']);
            continue; 
        end

        % Get the replayFileNames and sort them in trial order
        s = struct2cell(replaysFileList);
        replaysFileNames = natsortfiles(s(1,:));

        % Extract the scenario name from the replay filename
        expr = [mouseName '-D' dayNum '-([^-]+)-S([^-]+)-'];
        tokens = regexp(replaysFileList(1).name, expr, 'tokens');
        scenarioName = tokens{1}{1};
        sessionNum = tokens{1}{2};

        % Open the actions file for this mouse on this day, whose number of lines will match the number of 
        % replay files for that day.
        actionsFileName = [actionsFolder mouseName '-D' dayNum '-' scenarioName '-S' sessionNum '_actions.txt'];
        actionsFileID = fopen(actionsFileName);
        if (actionsFileID ~= -1)  % File was opened properly
            fgetl(actionsFileID); % Throw out the first line, as it is a column header
            actRecs = textscan(actionsFileID, actLineFormat); 
        else
            error(['Actions file ' actionsFileName 'could not be opened, so ending.']);
        end
        fclose(actionsFileID);  % If you forget to do this, then files no longer open and Matlab acts unpredictably

        % Use the scenario name to read the scenario file and then parse to draw the walls and targets.
        scenarioXDoc = xml2struct([scenariosFolder scenarioName '.xml']);
        % TODO: add support for multiple worlds, either embedded or pointed to
        % If the world is included, next read that file in
        if (isfield(scenarioXDoc.document.worlds, 'includeWorld')) 
            tempDOM = xml2struct([scenariosFolder scenarioXDoc.document.worlds.includeWorld(1).Text]);
            worldNode = tempDOM.world;
        else % world is embedded
            worldNode = scenarioXDoc.document.worlds.world(1);
        end
        
        % Next, identify which action records match the specified trial type
        filtRecIDs = [];
        for r_i=1:length(actRecs{1})
            stimIdx = getStimIdx(actRecs, r_i);
            if (isnan(stimIdx))  % Data is before I started recording stim indexes in the actions file, so figure it out
                stimLocX = getStimLoc(actRecs, r_i);
                stimIdx = mapLocXToIdx(stimLocX);
            end
            actionIdx = getActionIdx(actRecs, r_i);
            if (isnan(actionIdx))  % Data is before I started recording stim indexes in the actions file, so figure it out
                actionLocX = getActionLoc(actRecs, r_i);
                actionIdx = mapLocXToIdx(actionLocX);
            end
            isExtinctionTrial = getExtinction(actRecs, r_i);
            isCatchTrial = getCatch(actRecs, r_i);
            isCorrectionTrial = getCorrection(actRecs, r_i);

            if (trialTypeStrArr(tt_i) == "*-*" || ...
                (trialTypeStrArr(tt_i) == "L-*" && ~isExtinctionTrial && stimIdx == leftIdx) || ...
                (trialTypeStrArr(tt_i) == "L-L" && ~isExtinctionTrial && stimIdx == leftIdx && actionIdx == leftIdx) || ...
                (trialTypeStrArr(tt_i) == "L-R" && ~isExtinctionTrial && stimIdx == leftIdx && actionIdx == rightIdx) || ...
                (trialTypeStrArr(tt_i) == "L-S" && ~isExtinctionTrial && stimIdx == leftIdx && actionIdx == straightIdx) || ...
                (trialTypeStrArr(tt_i) == "R-*" && ~isExtinctionTrial && stimIdx == rightIdx) || ...
                (trialTypeStrArr(tt_i) == "R-L" && ~isExtinctionTrial && stimIdx == rightIdx && actionIdx == leftIdx) || ...
                (trialTypeStrArr(tt_i) == "R-R" && ~isExtinctionTrial && stimIdx == rightIdx && actionIdx == rightIdx) || ...
                (trialTypeStrArr(tt_i) == "R-S" && ~isExtinctionTrial && stimIdx == rightIdx && actionIdx == straightIdx) || ...
                (trialTypeStrArr(tt_i) == "S-*" && ~isExtinctionTrial && stimIdx == straightIdx) || ...
                (trialTypeStrArr(tt_i) == "S-L" && ~isExtinctionTrial && stimIdx == straightIdx && actionIdx == leftIdx) || ...
                (trialTypeStrArr(tt_i) == "S-R" && ~isExtinctionTrial && stimIdx == straightIdx && actionIdx == rightIdx) || ...
                (trialTypeStrArr(tt_i) == "S-S" && ~isExtinctionTrial && stimIdx == straightIdx && actionIdx == straightIdx) || ...
                ...
                (trialTypeStrArr(tt_i) == "LO-*" && isExtinctionTrial && stimIdx == leftIdx) || ...
                (trialTypeStrArr(tt_i) == "LO-L" && isExtinctionTrial && stimIdx == leftIdx && actionIdx == leftIdx) || ...
                (trialTypeStrArr(tt_i) == "LO-R" && isExtinctionTrial && stimIdx == leftIdx && actionIdx == rightIdx) || ...
                (trialTypeStrArr(tt_i) == "LO-S" && isExtinctionTrial && stimIdx == leftIdx && actionIdx == straightIdx) || ...
                (trialTypeStrArr(tt_i) == "RO-*" && isExtinctionTrial && stimIdx == rightIdx) || ...
                (trialTypeStrArr(tt_i) == "RO-L" && isExtinctionTrial && stimIdx == rightIdx && actionIdx == leftIdx) || ...
                (trialTypeStrArr(tt_i) == "RO-R" && isExtinctionTrial && stimIdx == rightIdx && actionIdx == rightIdx) || ...
                (trialTypeStrArr(tt_i) == "RO-S" && isExtinctionTrial && stimIdx == rightIdx && actionIdx == straightIdx) || ...
                (trialTypeStrArr(tt_i) == "Ca-*" && isCatchTrial) || ...
                (trialTypeStrArr(tt_i) == "Ca-L" && isCatchTrial && actionIdx == leftIdx) || ...
                (trialTypeStrArr(tt_i) == "Ca-R" && isCatchTrial && actionIdx == rightIdx) || ...
                (trialTypeStrArr(tt_i) == "Ca-S" && isCatchTrial && actionIdx == straightIdx) || ...
                ...
                (trialTypeStrArr(tt_i) == "LN-*"  && stimIdx == leftNearIdx) || ...
                (trialTypeStrArr(tt_i) == "LN-LN" && stimIdx == leftNearIdx && actionIdx == leftNearIdx) || ...
                (trialTypeStrArr(tt_i) == "LN-RN" && stimIdx == leftNearIdx && actionIdx == rightNearIdx) || ...
                (trialTypeStrArr(tt_i) == "LN-LF" && stimIdx == leftNearIdx && actionIdx == leftFarIdx) || ...
                (trialTypeStrArr(tt_i) == "LN-RF" && stimIdx == leftNearIdx && actionIdx == rightFarIdx) || ...
                (trialTypeStrArr(tt_i) == "RN-*"  && stimIdx == rightNearIdx) || ...
                (trialTypeStrArr(tt_i) == "RN-LN" && stimIdx == rightNearIdx && actionIdx == leftNearIdx) || ...
                (trialTypeStrArr(tt_i) == "RN-RN" && stimIdx == rightNearIdx && actionIdx == rightNearIdx) || ...
                (trialTypeStrArr(tt_i) == "RN-LF" && stimIdx == rightNearIdx && actionIdx == leftFarIdx) || ...
                (trialTypeStrArr(tt_i) == "RN-RF" && stimIdx == rightNearIdx && actionIdx == rightFarIdx) || ...
                (trialTypeStrArr(tt_i) == "LF-*"  && stimIdx == leftFarIdx) || ...
                (trialTypeStrArr(tt_i) == "LF-LN" && stimIdx == leftFarIdX && actionIdx == leftNearIdX) || ...
                (trialTypeStrArr(tt_i) == "LF-RN" && stimIdx == leftFarIdx && actionIdx == rightNearIdx) || ...
                (trialTypeStrArr(tt_i) == "LF-LF" && stimIdx == leftFarIdx && actionIdx == leftFarIdx) || ...
                (trialTypeStrArr(tt_i) == "LF-RF" && stimIdx == leftFarIdx && actionIdx == rightFarIdx) || ...
                (trialTypeStrArr(tt_i) == "RF-*"  && stimIdx == rightFarIdx) || ...
                (trialTypeStrArr(tt_i) == "RF-LN" && stimIdx == rightFarIdx && actionIdx == leftNearIdx) || ...
                (trialTypeStrArr(tt_i) == "RF-RN" && stimIdx == rightFarIdx && actionIdx == rightNearIdx) || ...
                (trialTypeStrArr(tt_i) == "RF-LF" && stimIdx == rightFarIdx && actionIdx == leftFarIdx) || ...
                (trialTypeStrArr(tt_i) == "RF-RF" && stimIdx == rightFarIdx && actionIdx == rightFarIdx) || ...
                (trialTypeStrArr(tt_i) == "Ca-*" && isCatchTrial) || ...
                (trialTypeStrArr(tt_i) == "Ca-LN" && isCatchTrial && actionIdx == leftNearIdx) || ...
                (trialTypeStrArr(tt_i) == "Ca-RN" && isCatchTrial && actionIdx == rightNearIdx) || ...
                (trialTypeStrArr(tt_i) == "Ca-LF" && isCatchTrial && actionIdx == leftFarIdx) || ...
                (trialTypeStrArr(tt_i) == "Ca-RF" && isCatchTrial && actionIdx == rightFarIdx) ...
                )
                    if (~isCorrectionTrial || (isCorrectionTrial && includeCorrectionTrials))
                        filtRecIDs(length(filtRecIDs)+1) = r_i;
                    end
            end
        end
                
        % There could be 1 more replay files than entries in the actions file if the game is manually ended, 
        % so limit the count to the number of rows in the actions files
        if (isempty(trials))
            trialsToDo = 1:length(filtRecIDs);
        else
            trialsToDo = trials(trials <= length(filtRecIDs));
        end
        for r_i=trialsToDo 
            stimIdx = getStimIdx(actRecs, filtRecIDs(r_i));
            if (isnan(stimIdx))  % Data is before I started recording stim indexes in the actions file, so figure it out
                stimLocX = getStimLoc(actRecs, filtRecIDs(r_i));
                stimIdx = mapLocXToIdx(stimLocX);
            end
            actionIdx = getActionIdx(actRecs, filtRecIDs(r_i));
            if (isnan(actionIdx))  % Data is before I started recording stim indexes in the actions file, so figure it out
                actionLocX = getActionLoc(actRecs, filtRecIDs(r_i));
                actionIdx = mapLocXToIdx(actionLocX);
            end
            optoLoc = getOptoLoc(actRecs, filtRecIDs(r_i));
            worldIdx = getWorldIdx(actRecs, filtRecIDs(r_i));
            isExtinctionTrial = getExtinction(actRecs, filtRecIDs(r_i));
            isCatchTrial = getCatch(actRecs, filtRecIDs(r_i));
            
            if (~drawOneFig)
                if (~useSubPlot)
                    f = initTrajFig(1, useSubPlot);
                    set(f, 'Position', [68*3 7*634/8 448 420])
                else
                    initTrajFig(1, useSubPlot);
                end
            end

            % Draw level map with walls and tree as a large circle
            if (~exist('wall', 'var') || ~drawOneFig)
                for w_i=1:length(worldNode.walls.wall)
                    wall = worldNode.walls.wall{w_i};
                    wallPosStr = wall.pos.Text;
                    wallPosXYZ = split(wallPosStr, ';');
                    wallPosX = str2double(wallPosXYZ{1});
                    wallPosZ = str2double(wallPosXYZ{3});
                    % Got center of wall, but need to get orientation and length
                    wallRotStr = wall.rot.Text;
                    wallRotXYZ = split(wallRotStr, ';');
                    wallRotY = -str2double(wallRotXYZ{2});  % Need to flip sign

                    wallScaleStr = wall.scale.Text;
                    wallScaleXYZ = split(wallScaleStr, ';');
                    wallScaleZ = str2double(wallScaleXYZ{3}) + 1; % add 1 because Unity does this

                    % Rotation matrix to rotate about the Y axis (though conventionally the Z axis)
                    Ry = [cosd(wallRotY) -sind(wallRotY); sind(wallRotY) cosd(wallRotY)];
                    x = [wallPosX, wallPosX];
                    z = [wallPosZ - 0.5*wallScaleZ, wallPosZ + 0.5*wallScaleZ];
                    % Need to shift to origin, then rotate, then shift back 
                    xCenter = wallPosX;
                    zCenter = wallPosZ;
                    x = x - xCenter;
                    z = z - zCenter;
                    rotatedWallPos = Ry*[x;z];
                    rotatedWallPos(1,:) = rotatedWallPos(1,:) + xCenter;
                    rotatedWallPos(2,:) = rotatedWallPos(2,:) + zCenter;

                    plot(rotatedWallPos(1,:), rotatedWallPos(2,:), 'Color', wallColor, 'LineWidth', wallWidth)
                end
                
                % After drawing walls, draw the tree visible on this trial.
                % Supports 3 and 4-choice trials
                % TODO: Support drawing correct tree width!
                for t_i=1:length(worldNode.trees.t)
                    treePosStr = worldNode.trees.t{t_i}.pos.Text;
                    treePosXYZ = split(treePosStr, ';');
                    treeScaleStr = worldNode.trees.t{t_i}.scale.Text;
                    treeScaleXYZ = split(treeScaleStr, ';'); 
                    plotTarget = 0;
                    if (length(worldNode.trees.t) == 3)
                        if (t_i == 1 && stimIdx == 0)
                            markerColor = shadingColorLeft;
                            plotTarget = 1;
                        elseif (t_i == 2 && stimIdx == 1)
                            markerColor = shadingColorRight;
                            plotTarget = 1;
                        elseif (t_i == 3)
                            if (~isCatchTrial && ~isExtinctionTrial && ~hideStraightTarget)
                                markerColor = shadingColorStraight;
                                plotTarget = 1;
                            end
                        end
                        
                        if (plotTarget)
                            % Simple code for plotting an ellipse
                            rx = (str2double(treeScaleXYZ{1}) + 1) * 4.5;  % For some reason the Unity code adds 1 - not how I would have done it!
                            rz = (str2double(treeScaleXYZ{3}) + 1) * 4.5;
                            x0 = str2double(treePosXYZ{1});
                            z0 = str2double(treePosXYZ{3});
                            t = -pi:0.01:pi;
                            x = x0 + rx*cos(t);
                            z = z0 + rz*sin(t);
                            patch(x, z, 'ok', 'LineWidth', 4, 'FaceColor', markerColor);
                            %if (treeScaleXYZ{1} == 0 && treeScaleXYZ{3} == 0)
                                % By default we plot targets as circles, unless specified otherwise
                            %    plot(str2double(treePosXYZ{1}), str2double(treePosXYZ{3}), 'ok', ...
                            %         'MarkerSize', 44, 'LineWidth', 4, 'MarkerFaceColor', markerColor);
                            %end
                        end
                    elseif (length(worldNode.trees.t) == 4)
                        markerColor = [-1 -1 -1];
                        if (t_i == 1 && stimIdx == 0)
                            markerColor = shadingColorLeft;
                        elseif (t_i == 2 && stimIdx == 1)
                            markerColor = shadingColorRight;
                        elseif (t_i == 3 && stimIdx == 2)
                            markerColor = shadingColorLeftFar;
                        elseif (t_i == 4 && stimIdx == 3)
                            markerColor = shadingColorRightFar;
                        end
                        
                        if (markerColor(1) ~= -1 && markerColor(2) ~= -1 && markerColor(3) ~= -1)
                            plot(str2double(treePosXYZ{1}), str2double(treePosXYZ{3}), 'ok', ...
                                 'MarkerSize', 44, 'LineWidth', 4, 'MarkerFaceColor', markerColor);
                        end
                    end
                    
                end
            end

            % Without cutting an extra 3 frames, the interpolation doesn't look right because the mouse is 
            % teleported back to the start before the trial ends           
            framesExtra = 3;
            if (~lastTrial)
                if (stimIdx == actionIdx)
                    cutFromEnd = correctDelay * fps + framesExtra; 
                else
                    cutFromEnd = incorrectDelay * fps + framesExtra;
                end
            else
                cutFromEnd = framesExtra;
            end
            
            %disp(['Processed replay #' num2str(filtRecIDs(r_i))]);

            % Finally, parse the replay file to draw the path the mouse took for this level.
            replaysFileID = fopen([replaysFileList(filtRecIDs(r_i)).folder '\' replaysFileNames{filtRecIDs(r_i)}]);
            if (replaysFileID ~= -1)  % File was opened properly
                C = textscan(replaysFileID, getReplayLineFormat(), 'Delimiter', {';', ','});
                trajX{end+1} = C{1}(1:length(C{3})-cutFromEnd);
                trajZ{end+1} = C{3}(1:end-cutFromEnd);
                if (length(trajX{end}) < minTrajLength)
                    minTrajLength = length(trajX{end}); % Store for resampling all trajectories later
                end
                
                % Sometimes the replay file has an x coord but no z coord, not sure why.
                % TODO: plotFilledEllipses(C{1}(1:length(C{3})-cutFromEnd), C{3}(1:end-cutFromEnd), markSize, markSize, 
                if (plotIndivTrials)
                    scatter(C{1}(1:length(C{3})-cutFromEnd), C{3}(1:end-cutFromEnd), markSize, ...
                        jet(length(C{3}(1:end-cutFromEnd))), 'filled', 'MarkerFaceAlpha', markAlpha, 'MarkerEdgeAlpha', markAlpha);
                end
                % Plot starting point (blue dot)
                plot(startX, startZ, 'o', 'MarkerSize', markSize, 'MarkerFaceColor', 'b', 'MarkerEdgeColor', 'w');
                fclose(replaysFileID);
            end
        end
        
        daysPlotted = daysPlotted + 1;
        if ~isempty(dayStr) % Prepend a comma if past the first day
            dayStr = [dayStr ','];
        end
        dayStr = [dayStr num2str(days(d_i))];
        numReplaysInFig = numReplaysInFig + length(filtRecIDs);
        
        clear wall;
        totalTrials = totalTrials + length(trialsToDo);
    end
    
    % Now that I've collected all the trajectories for the trials of interest, let me average them and plot the avg
    resizedTrajX = cellfun(@(x) interp1(1:length(x), x, 1:length(x)/minTrajLength:length(x))', trajX, 'UniformOutput', false);
    resizedTrajZ = cellfun(@(x) interp1(1:length(x), x, 1:length(x)/minTrajLength:length(x))', trajZ, 'UniformOutput', false);
    N = length(resizedTrajX);
    matX = cell2mat(resizedTrajX(:)');
    matZ = cell2mat(resizedTrajZ(:)');
    mX = nanmean(matX, 2);
    semX = std(matX, 0, 2)/sqrt(N);
    mZ = nanmean(matZ, 2);
    semZ = std(matZ, 0, 2)/sqrt(N);
    
    CI95 = tinv([0.025 0.975], N-1);
    xCI95 = bsxfun(@times, semX', CI95(:));
    zCI95 = bsxfun(@times, semZ', CI95(:));
    
    % plotFilledEllipses is a good replacement for scatter as it is size invariant
    if (~isempty(avgTrajColor))
        plotFilledEllipses(mX, mZ, xCI95(2,:), zCI95(2,:), avgTrajColor);
    end
    %scatter(mX, mZ, markSize, 'k', 'filled');
    
    % Plot title for all days of this trial type
    if(numReplaysInFig > 0)
        dayLabel = 'day';
        if (daysPlotted > 1)
            dayLabel = 'days';
        end
        tit = [upper(mouseName) ' (' dayLabel ' ' dayStr  '), ' trialTypeStrArr{tt_i} ', n=' ...
                num2str(totalTrials) ', ' corrTxt];
        title(tit);
    else % If not trajs plotted, close figure and reuse that position on the screen for the next figure
        close(f);
        figN = figN - 1;
    end
    numTrialsTotal = numTrialsTotal + numReplaysInFig;
end

if (drawOneFig)
    disp(['Total trials analyzed = ' num2str(totalTrials)]);
end

fclose('all');

end