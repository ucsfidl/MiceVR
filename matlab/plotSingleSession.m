function plotSingleSession(mouseName, days, sessions)
% This script takes an actions file, as defined with the params above, and
% plots the actions vs. the stimulus as a single row, color-coded.  A thin
% blue line on top shows whether optogenetics light is on or not.

centerX = 20000;

% First, find all the filenames to read in
fileList = dir(['data/*.txt']); % Get all mat files, and use that to construct filenames for video files

h = figure;
hold on
blockHeight = 1;
blockWidth = 1;

yMax = 1.2;

xLoc = 0;

colors = [  15 157 88;  % green
            226 50 50;  % red
            2 87 194;   % blue
            246 190 0; % orange
         ] / 255;
     
blueLightColor = [ 29 255 255 ] / 255;

corrOverTime = zeros(3,1);
totalOverTime = zeros(3,1);

numFilesAnalyzed = 0;
for i=1:length(fileList)
    for j=1:length(days)
        if (contains(fileList(i).name, [mouseName '-D' num2str(days(j))]))
            matchesSession = false;
            if isempty(sessions)
                matchesSession = true;
            else
                for m=1:length(sessions)
                    if (contains(fileList(i).name, ['-S' num2str(sessions(m))]))
                        matchesSession = true;
                    end
                end
            end
            if (matchesSession)
                filename = fileList(i).name;
                fid = fopen([fileList(i).folder '\' filename]);
                if (fid ~= -1)  % File was opened properly
                    numFilesAnalyzed = numFilesAnalyzed + 1;
                    tline = fgetl(fid); % Throw out the first line, as it is a column header
                    C = textscan(fid, '%s %s %d %s %d %d %d %d %d %d %d %d %d %d %f %d %d'); % C is a cell array with each string separated by a space
                    for k = 1:length(C{1})  % For each line
                        % C{5} is the target location, C{12} is the turn location
                        stimLoc = C{5}(k);
                        actionLoc = C{12}(k);
                        optoLoc = C{17}(k);
                        
                        % Supports 3-choice for now - support 4-choice later.
                        if (stimLoc < centerX)
                            corrIdx = 1;
                        elseif (stimLoc > centerX)
                            corrIdx = 2;
                        else
                            corrIdx = 3;
                        end
                        
                        c = colors(corrIdx,:);
                        
                        x = [xLoc xLoc+1 xLoc+1 xLoc];
                        y = [blockHeight blockHeight blockHeight*2 blockHeight*2];
                        patch(x,y,c, 'EdgeColor', 'none');
                        
                        if (actionLoc < centerX)
                            c = colors(1,:);
                        elseif (actionLoc > centerX)
                            c = colors(2,:);
                        elseif (actionLoc == centerX)
                            c = colors(3,:);
                        end
                        
                        y = [0 0 blockHeight blockHeight];
                        patch(x,y,c, 'EdgeColor', 'none');
                        
                        y = [blockHeight*2 blockHeight*2 blockHeight*2.3 blockHeight*2.3];
                        if (optoLoc > -1)
                            patch(x,y,blueLightColor, 'EdgeColor', 'none');
                        else
                            patch(x,y,'k', 'EdgeColor', 'none');
                        end
                        
                        xLoc = xLoc + blockWidth;
                        
                        % Track accuracy, to plot at the end
                        corrOverTime(:, end+1) = corrOverTime(:, end);
                        totalOverTime(:, end+1) = totalOverTime(:, end);
                        totalOverTime(corrIdx, end) = totalOverTime(corrIdx, end) + 1;
                        if (stimLoc == actionLoc)  % Correct trial
                            corrOverTime(corrIdx, end) = corrOverTime(corrIdx, end) + 1;
                        end
                    end
                end
                ylim([0, 2.3*blockHeight]);
                fclose(fid);
            end
        end
    end
end

figure
hold on
for i=1:3
    plot(1:size(totalOverTime(i,:), 2), corrOverTime(i,:) ./ totalOverTime(i,:), 'Color', colors(i,:), 'LineStyle', '-', 'Marker', 'o', 'LineWidth', 2, 'MarkerSize', 2);
end
ylim([0 yMax]);
title(filename, 'Interpreter', 'none');

disp(['Analyzed ' num2str(numFilesAnalyzed) ' files.']);

end