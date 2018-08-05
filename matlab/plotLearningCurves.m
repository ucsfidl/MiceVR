function plotLearningCurves()
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

docid = '1X75Ckw-l5QzzpPPgbrmRsZCOvaNGkEdSk8txUEq53nY';

[screenWidth, screenHeight] = getScreenDim();
screenOffsetX = 70;
screenOffsetY = 70;

% Column correspondences
DAY = 1;
SCENARIO = 2;
ACCURACY = 19;
MOUSEDATA = 27;

sheetIDs = GetSheetIDs(docid, 1);   % Data sheets only, not all sheets

yMax = 119;
yMin = 0;
levelChangeLineColor = [0.3 0.3 0.3];
yLevelLabel = [105 110 115];
levelLabelSize = 8;
labelOffset = 0.2;  % x offset for level change label

yln = length(yLevelLabel);

criterion = 80;
criterionColor = [0.5 0.5 0.5];

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
        lev = sheet(row, SCENARIO);
        if (isstrprop(d, 'digit') & ~isempty(a)) 
            xAcc(end+1) = str2double(sheet{row, 1});
            acc(end+1) = str2double(a(1:end-1));  % Trim off the percent
            if (strcmp(lastLevel, lev) == 0)  % If changed levels, record the change
                xLevelChange(end+1) = xAcc(end);
                levelChange(end+1) = lev;
            end
            lastLevel = lev;
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
        
    % First, plot the average accuracy over training days
    h = figure;
    hold on;
    plot(xAcc, acc, 'k-o', 'LineWidth', 2, 'MarkerFaceColor', 'k', 'MarkerSize', 3);
    for i=1:length(xLevelChange)
        plot(repmat(xLevelChange(i)-0.5, 1, 2), [yMin yMax], 'Color', levelChangeLineColor);
        if (i > 1 && xLevelChange(i) == xLevelChange(i-1))
            yLabel = yLevelLabel(2);
        else
            yLabel = yLevelLabel(1);
        end
        text(xLevelChange(i) + labelOffset - 0.5, yLabel, levelChange(i), ...
            'Interpreter', 'none', 'FontSize', levelLabelSize);
    end
    % Figure trimmings
    title([name ': ' strain ', ' experiment]);
    ylabel('Overall accuracy (%)');
    xlabel('Training day');
    ylim([yMin yMax]);
    xl = xlim;
    xlim([1 xl(2)]);
    xl = xlim;
    % plot criterion line
    plot([xl(1) xl(2)], repmat(criterion, 1, 2), '--', 'Color', criterionColor);
    % place the figure in the right place
    xm = mod(sheetIdx-1, 3);
    ym = mod(floor((sheetIdx-1) / 3), 2);    
    set(h, 'Position', [screenOffsetX+h.Position(3)*xm ...
                        screenHeight-h.Position(4)-screenOffsetY-(h.Position(4)+screenOffsetY)*ym ...
                        h.Position(3) h.Position(4)]);
    
    % Second, plot the per choice accuracy over training ddays
end

end