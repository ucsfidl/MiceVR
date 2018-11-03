function results = getStats(mouseName, days, sessions)
% This function will analyze the relevant actions.txt log files and return
% a set of statistics useful to analyzing blindness and blindsight.

centerX = 20000;

% first column is all Left stim trials
% second column is all Right stim trials
% third column is all Center stim trials

% first row is all Left action trials
% second row is all Right action trials
% third row is all Center action trials

results = zeros(3,3,4);  % First is non-opto, second is optoL, third is optoR, and fourth is optoBoth - sum to get all
leftStimStraightErrorsMap = containers.Map();
rightStimStraightErrorsMap = containers.Map();

% First, find all the filenames to read in
fileList = dir(['*.txt']); % Get all mat files, and use that to construct filenames for video files

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
                fid = fopen([fileList(i).folder '\' fileList(i).name]);
                if (fid ~= -1)  % File was opened properly
                    numFilesAnalyzed = numFilesAnalyzed + 1;
                    tline = fgetl(fid); % Throw out the first line, as it is a column header
                    C = textscan(fid, '%s %s %d %s %d %d %d %d %d %d %d %d %d %d %f %d %d %d %d'); % C is a cell array with each string separated by a space
                    for k = 1:length(C{1})  % For each line
                        % C{5} is the target location, C{12} is the turn location
                        stimLoc = C{5}(k);
                        actionLoc = C{12}(k);
                        optoLoc = C{17}(k);

                        if (stimLoc < centerX)
                            col = 1;
                        elseif (stimLoc > centerX)
                            col = 2;
                        elseif (stimLoc == centerX)
                            col = 3;
                        end

                        if (actionLoc < centerX)
                            row = 1;
                        elseif (actionLoc > centerX)
                            row = 2;
                        elseif (actionLoc == centerX)
                            row = 3;
                        end

                        % Put trials in correct sheet
                        results(row, col, optoLoc + 2) = results(row, col, optoLoc + 2) + 1;

                        if (col ~= row)  % error trial
                            nasal = C{8}(k);
                            temporal = C{9}(k);
                            high = C{10}(k);
                            low = C{11}(k);
                            key = ['N' num2str(nasal) '_T' num2str(temporal) '_H' num2str(high) '_L' num2str(low)];
                            prevVal = 0;
                            if (actionLoc == centerX)
                                if (stimLoc < centerX)
                                    if (isKey(leftStimStraightErrorsMap, key))
                                        prevVal = leftStimStraightErrorsMap(key);
                                    end
                                    leftStimStraightErrorsMap(key) = prevVal + 1; %#ok<*NASGU>
                                elseif (stimLoc > centerX)
                                    if (isKey(rightStimStraightErrorsMap, key))
                                        prevVal = rightStimStraightErrorsMap(key);
                                    end
                                    rightStimStraightErrorsMap(key) = prevVal + 1; %#ok<*NASGU>
                                end
                            end
                        end
                    end
                end
                fclose(fid);
            end
        end
    end
end

for j = 1:size(results,3)
    if (j == 1) 
        disp('=====Non-Opto======')
    elseif (j == 2)
        disp('=====Opto Left======')        
    elseif (j == 3)
        disp('=====Opto Right======')        
    elseif (j == 4)
        disp('=====Opto Both======')        
    end
    disp(['L->L = ' num2str(results(1,1,j) / sum(results(:,1,j)) * 100, 2) '%']);
    disp(['L->R = ' num2str(results(2,1,j) / sum(results(:,1,j)) * 100, 2) '%']);
    disp(['L->C = ' num2str(results(3,1,j) / sum(results(:,1,j)) * 100, 2) '%']);
    disp('-----------')
    disp(['R->L = ' num2str(results(1,2,j) / sum(results(:,2,j)) * 100, 2) '%']);
    disp(['R->R = ' num2str(results(2,2,j) / sum(results(:,2,j)) * 100, 2) '%']);
    disp(['R->C = ' num2str(results(3,2,j) / sum(results(:,2,j)) * 100, 2) '%']);
    disp('-----------')
    disp(['C->L = ' num2str(results(1,3,j) / sum(results(:,3,j)) * 100, 2) '%']);
    disp(['C->R = ' num2str(results(2,3,j) / sum(results(:,3,j)) * 100, 2) '%']);
    disp(['C->C = ' num2str(results(3,3,j) / sum(results(:,3,j)) * 100, 2) '%']);
    disp('===========')
end

disp(['Analyzed ' num2str(numFilesAnalyzed) ' files.']);

disp('Stim on left, but mouse goes straight:');
disp(keys(leftStimStraightErrorsMap));
disp(values(leftStimStraightErrorsMap));

disp('Stim on right, but mouse goes straight:');
disp(keys(rightStimStraightErrorsMap));
disp(values(rightStimStraightErrorsMap));

end