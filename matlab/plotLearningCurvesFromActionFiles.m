function plotLearningCurvesFromActionFiles(mouseName, days, sessions)
% This function takes a mouseName and start and end days, and plots the
% learning curves, including separate curves for optogenetics on or off.
%
% We need this for optogenetic learning curves as the Google Spreadsheet
% does not have any knowledge of the optogenetic performance.
%
% If sessions is specified, it should be a vector the same length as days
% and correspond to each day.

if (~isempty(sessions) && length(days) ~= length(sessions))
    error('Please provide a session number for each corresponding day');
end

centerX = 20000;

[screenWidth, screenHeight] = getScreenDim();
screenOffsetX = 70;
screenOffsetY = 70;

colorLeft = [2 87 194]/255;  % blue  - opto will be a dotted line and open circle
colorRight = [226 50 50]/255; % red
colorCenter = [15 157 88]/255;  % green

colorLeftFar = [2 193 194]/255; % cyan
colorRightFar = [246 190 0]/255;  % orange

% For each day, find the actions file and parse it to get the accuracy for
% that day, separated by stimulus location and optogenetic status
% Assumes that the current working directory is where it should look.
numFilesAnalyzed = 0;
fileList = dir(strcat(mouseName, '*actions.txt'));
num = zeros(6, length(days));  % 3 for opto off, 3 for opto on
denom = zeros(6, length(days));
for i=1:length(days)
    for j=1:length(fileList)
        sessionStr = '';
        if (~isempty(sessions))
            sessionStr = ['.*S' num2str(session(i)) '_actions.txt'];
        end
        
        if (regexp(fileList(j).name, [mouseName '-D' num2str(days(i)) sessionStr]) == 1)
            filename = fileList(i).name;
            fid = fopen([fileList(i).folder '\' filename]);
            if (fid ~= -1)  % File was opened properly
                disp(['Analyzed ' filename]);
                numFilesAnalyzed = numFilesAnalyzed + 1;
                tline = fgetl(fid); % Throw out the first line, as it is a column header
                C = textscan(fid, '%s %s %d %s %d %d %d %d %d %d %d %d %d %d %f %d %d %d %d'); % C is a cell array with each string separated by a space
                for k = 1:length(C{1})  % For each line
                    % C{5} is the target location, C{12} is the turn location
                    stimLoc = C{5}(k);
                    actionLoc = C{12}(k);
                    optoLoc = C{17}(k);
                    if (optoLoc == -1)
                        optoMult = 0;
                    else
                        optoMult = 1;
                    end
                    if (stimLoc < centerX)
                        idx = 1 + 3*optoMult;
                    elseif (stimLoc > centerX)
                        idx = 2 + 3*optoMult;
                    else
                        idx = 3 + 3*optoMult;
                    end
                    denom(idx, days(i)) = denom(idx, days(i)) + 1;
                    if (stimLoc == actionLoc)
                        num(idx, days(i)) = num(idx, days(i)) + 1;
                    end
                end
            break;
        end
    end
end

end