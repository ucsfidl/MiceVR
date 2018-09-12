function results = getStats(mouseName, days)
% This function will analyze the relevant actions.txt log files and return
% a set of statistics useful to analyzing blindness and blindsight.

leftX = 19980;
rightX = 20020;
centerX = 20000;

% first column is all Left stim trials
% second column is all Right stim trials
% third column is all Center stim trials

% first row is all Left action trials
% second row is all Right action trials
% third row is all Center action trials

results = zeros(3,3);

% First, find all the filenames to read in
fileList = dir(['data/*.txt']); % Get all mat files, and use that to construct filenames for video files

numFilesAnalyzed = 0;
for i=1:length(fileList)
    for j=1:length(days)
        if (contains(fileList(i).name, [mouseName '-D' num2str(days(j))]))
            fid = fopen([fileList(i).folder '\' fileList(i).name]);
            if (fid ~= -1)  % File was opened properly
                numFilesAnalyzed = numFilesAnalyzed + 1;
                tline = fgetl(fid); % Throw out the first line, as it is a column header
                C = textscan(fid, '%s %s %d %s %d %d %d %d %d %d %d %d %d %d %f %d %d'); % C is a cell array with each string separated by a space
                for k = 1:length(C{1})  % For each line
                    % C{5} is the target location, C{12} is the turn location
                    stimLoc = C{5}(k);
                    actionLoc = C{12}(k);
                    
                    nasal = C{8}(k);
                    temporal = C{9}(k);
                    high = C{10}(k);
                    low = C{11}(k);

                    if (stimLoc == leftX)
                        col = 1;
                    elseif (stimLoc == rightX)
                        col = 2;
                    elseif (stimLoc == centerX)
                        col = 3;
                    end
                    
                    if (actionLoc == leftX)
                        row = 1;
                    elseif (actionLoc == rightX)
                        row = 2;
                    elseif (actionLoc == centerX)
                        row = 3;
                    end
                    
                    results(row, col) = results(row, col) + 1;
                end
            end
        end
    end
end

disp('===========')
disp(['L->L = ' num2str(results(1,1) / sum(results(:, 1)) * 100, 2) '%']);
disp(['L->R = ' num2str(results(2,1) / sum(results(:, 1)) * 100, 2) '%']);
disp(['L->C = ' num2str(results(3,1) / sum(results(:, 1)) * 100, 2) '%']);
disp('-----------')
disp(['R->L = ' num2str(results(1,2) / sum(results(:, 2)) * 100, 2) '%']);
disp(['R->R = ' num2str(results(2,2) / sum(results(:, 2)) * 100, 2) '%']);
disp(['R->C = ' num2str(results(3,2) / sum(results(:, 2)) * 100, 2) '%']);
disp('-----------')
disp(['C->L = ' num2str(results(1,3) / sum(results(:, 3)) * 100, 2) '%']);
disp(['C->R = ' num2str(results(2,3) / sum(results(:, 3)) * 100, 2) '%']);
disp(['C->C = ' num2str(results(3,3) / sum(results(:, 3)) * 100, 2) '%']);
disp('===========')


disp(['Analyzed ' num2str(numFilesAnalyzed) ' files.']);

end