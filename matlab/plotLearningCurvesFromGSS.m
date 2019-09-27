function plotLearningCurvesFromGSS(docid, mouseNames, startDays, endDays)
% Takes a Google Spreadsheet, and plots the learning curves.
% Each tab is a mouse, and 2 plots are generated per moues:
% (1) Overall accuracy on the session
% (2) Accuracy per choice on each session
% In both graphs, changes in the level will be marked with a vertical line,
% ideally with the name of the level labeling the line (or available as a
% tooltip on mouseOver). Also, changes in the field restriction will also
% result in a line being drawn, so the viewer is not under the mistaken
% impression that the mouse performed worse because they had forgotten
% something they previously knew.

% My Berkeley sheets are at 1X75Ckw-l5QzzpPPgbrmRsZCOvaNGkEdSk8txUEq53nY
% I enabled anyone can view with link privileges, to get this to work.

% This program uses GetGoogleSpreadsheet, from the File Exchange, to read
% the spreadsheet from the web into Matlab memory.

% Set the docid below to your sheet's unique id in its URL

% If no mouseName specified, print all (e.g. for weekly 1on1)

% Usage:
% > plotLearningCurves('16RLlczw1EiOIwSSsFLb0rNxkvgFp46ygZCojvGBg084', {'Icarus'}, [], [])

% IMPORTANT: names in mouseNames must be in order of the tabs found, else
% the startDays and endDays will not correspond correctly to the correct
% sheet/mouse.

[screenWidth, screenHeight] = getScreenDim();
screenOffsetX = 170;
screenOffsetY = 170;

colorLeft = [2 87 194]/255;  % blue
colorRight = [226 50 50]/255; % red
colorCenter = [15 157 88]/255;  % green

colorLeftFar = [2 193 194]/255; % cyan
colorRightFar = [246 190 0]/255;  % orange

% Column correspondences
DAY = 1;
SCENARIO = 2;
WEIGHT = 8;  % Track this, as we will run multiple trials on one day, but not update the weight, so use this to know that the session should not be plotted
ACCURACY = 19;
MOUSEDATA = 27;
TRIALSPERMIN = 18;
NASAL = 4;
TEMPORAL = 5;
HIGH = 6;
LOW = 7;
RESULTS = 12;

sheetIDs = GetSheetIDs(docid, mouseNames, 1);   % Data sheets only, not all sheets

levelChangeLineColor = [0.3 0.3 0.3];
tpmColor = [0.7 0.7 0.7];
tpmMax = 20;  % Max value for the right TPM axis
yLevelLabel = [105 110 115 120 125 130];
levelLabelSize = 8;
xLabelOffset = 0.2;  % x offset for level change label
yMax = yLevelLabel(end) + 4;
yMin = 0;

yln = length(yLevelLabel);

criterion = 80;
criterionColor = [0.5 0.5 0.5];
tpmCrit = 4;

for sheetIdx=1:length(sheetIDs)
    sheet = GetGoogleSpreadsheet(docid, sheetIDs(sheetIdx));  % sheet is a cell array of the spreadsheet
    
    % Now that I have the sheet, extract the:
    % (1) Overall accuracy
    % (2) Position-specific accuracy
    % (3) Level changes with training day
    % If multiple levels were on the same training day, plot each of them
    % on that day.
    % The sheet is expected to have the following format:
    % Day, Scenario, Session, N, T, H, L, Weight (g), W %, BC, Max H2O,
    % Results, Date, Dur (m), Rewards, R/min, Trials, T/min, Accuracy.
    % Accuracy is the overall accuracy, and Results is the choice-specific
    % accuracy.
    xAcc = [];
    acc = [];
    tpm = []; % Plot trials/min on the right axis
    indivAcc = {};  % Store up to 4 values (2- to 4-choice), with empty values as NaN
    xLevelChange = [];
    levelChange = {};
    lastLevel = '';
    name = '';
    strain = '';
    experiment = '';
        
    for row=1:size(sheet, 1)
        % If all characters are digits, this is a valid number and we found the first valid line
        d = sheet{row, DAY};
        a = sheet{row, ACCURACY};
        w = sheet{row, WEIGHT};
        t = sheet{row, TRIALSPERMIN};
        lev = sheet(row, SCENARIO);
        na = sheet(row, NASAL);
        te = sheet(row, TEMPORAL);
        hi = sheet(row, HIGH);
        lo = sheet(row, LOW);
        r = sheet(row, RESULTS);
            
        %if (str2double(d) == 112)
            %disp(d);
        %end
        
        if (isstrprop(d, 'digit') & ~isempty(a) & ~isempty(w))
            if (isempty(startDays) || ...
                    (str2double(d) >= startDays(sheetIdx) && (isempty(endDays) || str2double(d) <= endDays(sheetIdx))))
                xAcc(end+1) = str2double(d);
                acc(end+1) = str2double(a(1:end-1));  % Trim off the percent
                tpm(end+1) = str2double(t);
                
                % Update level name with restriction
                if (~isempty(na{1}))
                    lev{1} = [lev{1} '_n' na{1}];
                end
                if (~isempty(te{1}))
                    lev{1} = [lev{1} '_t' te{1}];
                end
                if (~isempty(hi{1}))
                    lev{1} = [lev{1} '_h' hi{1}];
                end
                if (~isempty(lo{1}))
                    lev{1} = [lev{1} '_l' lo{1}];
                end
                                
                % Parse the category accuracies
                subResultsText = strsplit(r{1}, '/');
                subResults = [];
                for m=2:length(subResultsText)  % Skip the first entry, as that is the overall accuracy
                    subResults(end+1) = str2num(subResultsText{m});
                end
                                
                if (isempty(indivAcc) || strcmp(lastLevel, lev) == 0)
                    indivAcc{end+1} = subResults';
                else
                    indivAcc{end} = cat(2, indivAcc{end}, subResults');
                end
                
                if (strcmp(lastLevel, lev) == 0)  % If changed levels, record the change
                    xLevelChange(end+1) = xAcc(end);
                    levelChange(end+1) = lev;
                end
                lastLevel = lev;
            end
        end
        
        mousedata = sheet{row, MOUSEDATA};
        if (row == 1) % First row always has mouse's name
            name = mousedata;
        elseif (strcmp(mousedata, 'Strain'))
            strain = sheet{row, MOUSEDATA+1};
        elseif (strcmp(mousedata, 'Experiment'))
            experiment = sheet{row, MOUSEDATA+1};
        end
    end

    %disp(['processed ' name]);

    % First, plot the trials/min, to guage motivation
    h = figure;
    hold on;
    yyaxis right % plot to left y axis
    plot(xAcc, tpm, 'Color', tpmColor);
    ylabel('Trial speed (trials/min)');
    ax = gca;
    ax.YColor = tpmColor;
    ylim([0 tpmMax]);
    % plot criterion line
    xl = xlim;
    plot([xl(1) xl(2)], repmat(tpmCrit, 1, 2), ':', 'Color', tpmColor);
    
    % Second, plot average accuracy over training days on the left axis
    % Plot second so it overlaps the trials/min
    yyaxis left % plot to left y axis
    % UNCOMMENT if want to see overall performance average
    % plot(xAcc, acc, 'k-o', 'LineWidth', 2, 'MarkerFaceColor', 'k', 'MarkerSize', 3);

    % Third, go through each subResult and draw the lines of those!
    yStartInd = 1;
    for i=1:length(indivAcc)
        yEndInd = yStartInd + size(indivAcc{i}, 2) - 1;
        for j=1:size(indivAcc{i}, 1)
            if (size(indivAcc{i}, 1) == 1)
                c = colorCenter;
            elseif (size(indivAcc{i},1) == 2)
                if (contains(levelChange(i), "LC"))
                    if (j == 1)
                        c = colorLeft;
                    else
                        c = colorCenter;
                    end
                elseif (contains(levelChange(i), "RC"))
                    if (j == 1)
                        c = colorRight;
                    else
                        c = colorCenter;
                    end     
                else
                    if (j == 1)
                        c = colorLeft;
                    else
                        c = colorRight;
                    end
                end
            elseif (size(indivAcc{i}, 1) == 3)
                if (j == 1)
                    c = colorLeft;
                elseif (j == 2)
                    c = colorRight;
                else
                    c = colorCenter;
                end
            elseif (size(indivAcc{i}, 1) == 4)
                if (j == 1)
                    c = colorLeft;
                elseif (j == 2)
                    c = colorRight;
                elseif (j == 3)
                    c = colorLeftFar;
                else
                    c = colorRightFar;
                end
            end
            if (xAcc(yStartInd) == 155)
                disp('')
            end
            plot(xAcc(yStartInd):xAcc(yEndInd), indivAcc{i}(j,:), 'LineStyle', '-', 'Color', c, 'Marker', 'o', 'MarkerFaceColor', c, 'LineWidth', 2, 'MarkerSize', 3);
        end
        yStartInd = yEndInd + 1;
    end
    
    % Fourth, plot level change lines
    for i=1:length(xLevelChange)
        plot(repmat(xLevelChange(i)-0.5, 1, 2), [yMin yMax], '-', 'Color', levelChangeLineColor);
    end
    
    % Fifth, plot labels for the level change lines
    for i=1:length(xLevelChange)
        if (i == 1)
            yLabel = yLevelLabel(1);
            yLastLevelIdx = 1;
        %elseif (i > 1 && xLevelChange(i) == xLevelChange(i-1))
        %    yLabel = yLevelLabel(2);
        %       yLastLevelIdx = 2;
        else
            yNextLevelIdx = mod(yLastLevelIdx, yln) + 1;  % Pick new y level for next level
            yLabel = yLevelLabel(yNextLevelIdx);
            yLastLevelIdx = yNextLevelIdx;
        end
        text(xLevelChange(i) + xLabelOffset - 0.5, yLabel, levelChange(i), ...
            'Interpreter', 'none', 'FontSize', levelLabelSize, 'Margin', 0.1, 'BackgroundColor', 'w');
    end
    
    % Figure labels
    title([name ': ' strain ', ' experiment]);
    ylabel('Accuracy (%)', 'Color', 'k');
    %set(get(gca,'ylabel'),'rotation',0)
    xlabel('Training day');
    ylim([yMin yMax]);
    ax = gca;
    ax.YColor = 'k';
    % Remove y-axis values and tickmarks greater than 100
    yticks([0:20:100])
    
    % plot criterion line
    %xl = xlim;
    xlim([min(xAcc)-1 max(xAcc)]);
    xl = xlim;
    plot([xl(1) xl(2)], repmat(criterion, 1, 2), '--', 'Color', criterionColor);
        
    % place the figure in the right place
    xm = mod(sheetIdx-1, 3);
    ym = mod(floor((sheetIdx-1) / 3), 2);    
    set(h, 'Position', [screenOffsetX+h.Position(3)*xm ...
                        screenHeight-h.Position(4)-screenOffsetY-(h.Position(4)+screenOffsetY)*ym ...
                        900 450]);
    
    % Second, plot the per choice accuracy over training ddays
end

end