function plotLearningCurves(mouseNames, startDays, endDays)
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
% > plotLearningCurves({}];
% > plotLearningCurves({'Andor', 'Crinkle'});
% > plotLearningCurves({'Xylo' 'Lymph'}, [112 178]);
% > plotLearningCurves({'Andor', 'Birdy', 'Crinkle', 'Daria', 'Eureka', 'Funicular', 'Gertie', 'Haiku', 'Ingersol', 'Jolly', 'Krazy', 'Lemur', 'Mnm', 'Quite', 'Octo', 'Palor'}, [], [])
% > plotLearningCurves({'Xylo', 'Lymph', 'Cryo', 'Berlin', 'Alpha', 'Venom', 'Zizzle', 'Yum', 'Ollie', 'Nasty', 'Inta', 'Umpa', 'Quasar', 'Candy', 'Roxie'}, [112 178 197, 80, 212, 117, 106, 113, 145, 144, 175, 117, 143, 278, 134], [])

% IMPORTANT: names in mouseNames must be in order of the tabs found, else
% the startDays and endDays will not correspond correctly to the correct
% sheet/mouse.

docid = '1X75Ckw-l5QzzpPPgbrmRsZCOvaNGkEdSk8txUEq53nY';

[screenWidth, screenHeight] = getScreenDim();
screenOffsetX = 70;
screenOffsetY = 70;

% Column correspondences
DAY = 1;
SCENARIO = 2;
ACCURACY = 19;
MOUSEDATA = 27;
TRIALSPERMIN = 18;
NASAL = 4;
TEMPORAL = 5;
HIGH = 6;
LOW = 7;
RESULTS = 12;

sheetIDs = GetSheetIDs(docid, mouseNames, 1);   % Data sheets only, not all sheets

yMax = 119;
yMin = 0;
levelChangeLineColor = [0.3 0.3 0.3];
tpmColor = [0.7 0.7 0.7];
tpmMax = 20;  % Max value for the right TPM axis
yLevelLabel = [105 110 115];
levelLabelSize = 8;
xLabelOffset = 0.2;  % x offset for level change label

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
    indivAcc = [];  % Store up to 4 values (2- to 4-choice), with empty values as NaN
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
        t = sheet{row, TRIALSPERMIN};
        lev = sheet(row, SCENARIO);
        na = sheet(row, NASAL);
        te = sheet(row, TEMPORAL);
        hi = sheet(row, HIGH);
        lo = sheet(row, LOW);
        r = sheet(row, RESULTS);
            
        if (str2double(d) == 112)
            %disp(d);
        end
        
        if (isstrprop(d, 'digit') & ~isempty(a))
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
    plot(xAcc, acc, 'k-o', 'LineWidth', 2, 'MarkerFaceColor', 'k', 'MarkerSize', 3);

    % Second, plot level change lines
    for i=1:length(xLevelChange)
        plot(repmat(xLevelChange(i)-0.5, 1, 2), [yMin yMax], '-', 'Color', levelChangeLineColor);
    end
    
    % Third, plot labels for the level change lines
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
    ylabel('Overall accuracy (%)', 'Color', 'k');
    xlabel('Training day');
    ylim([yMin yMax]);
    ax = gca;
    ax.YColor = 'k';
    
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
                        h.Position(3) h.Position(4)]);
    
    % Second, plot the per choice accuracy over training ddays
end

end