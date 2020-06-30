function results = getStatsAngleDiscrimination(mouseName, days, sessions, distractorAngles, includeCorrectionTrials)
% This function will analyze the relevant actions.txt log files and return
% a set of statistics useful to analyzing blindness and blindsight.

% USAGE:
% Make sure you are in the data directory containing the actions files you want to analyze (e.g. UCSF/data)
% > getStatsAngleDiscrimination('Xochi', [72], [], [45 15 -45 -15], 0)

% distractor N: left correct, right correct, total trials
centerX = 20000;
results = zeros(length(distractorAngles),4);

% First, find all the filenames to read in
fileList = dir(['*actions.txt']); % Get all mat files, and use that to construct filenames for video files

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
                    C = textscan(fid, '%s %s %d %s %s %d %d %d %d %d %d %s %d %d %f %d %d %d %d %d %d'); % C is a cell array with each string separated by a space
                    for k = 1:length(C{1})  % For each line
                        targetAngle = C{18}(k);
                        turnAngle = C{19}(k);
                        distractorAngle = C{20}(k);
                        % C{5} is the target location, C{12} is the turn location
                        if (iscell(C{5}(k)))
                            tmp = strsplit(C{5}{k}, ';');
                            stimLoc = str2double(tmp{1});
                        else
                            stimLoc = str2double(C{5}(k));
                        end

                        isCorrectionTrial = C{21}(k);
                        if (isCorrectionTrial && ~includeCorrectionTrials)
                            continue;
                        end
                        
                        for m = 1:length(distractorAngles)
                            if (distractorAngles(m) == distractorAngle)
                                row = m;
                                break;
                            end
                        end

                        if (stimLoc < centerX)
                            results(row, 2) = results(row, 2) + 1;
                            if (turnAngle == targetAngle)
                                results(row, 1) = results(row, 1) + 1;
                            end
                        else
                            results(row, 4) = results(row, 4) + 1;
                            if (turnAngle == targetAngle)
                                results(row, 3) = results(row, 3) + 1;
                            end                            
                        end
                    end
                end
                fclose(fid);
            end
        end
    end
end

accuracy_summary_text = [];
for j = 1:size(results,1)
    accuracy_summary_text = [accuracy_summary_text num2str((results(j,1) + results(j,3)) / (results(j,2)+results(j,4)) * 100, 2)];
    if (j < size(results,1))
        accuracy_summary_text = [accuracy_summary_text '/'];
    end
    disp(['Condition #' num2str(j) ': ' num2str(distractorAngles(j)) ' degree distractor']);
    disp(['Overall = ' num2str((results(j,1) + results(j,3)) / (results(j,2)+results(j,4)) * 100, 2) '%']);
    disp(['Left = ' num2str(results(j,1) / results(j,2) * 100, 2) '%']);
    disp(['Right = ' num2str(results(j,3) / results(j,4) * 100, 2) '%']);
    disp(['hit rate = '  num2str(round(results(j,1) / results(j,2), 2)*100) '%']);
    disp(['false alarm rate = ' num2str(round((results(j,4)-results(j,3)) / results(j,4), 2)*100) '%']);
    disp(['d'' = ' num2str(round(1/sqrt(2) * (norminv(results(j,1) /results(j,2)) - norminv((results(j,4)-results(j,3)) / results(j,4))), 2))]);
    disp('-----------')
end

disp('===========')
disp(accuracy_summary_text);  
disp('===========')

disp(['Analyzed ' num2str(numFilesAnalyzed) ' files.']);

end