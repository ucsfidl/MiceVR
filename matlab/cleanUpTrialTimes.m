function cleanUpTrialTimes(videoMetadataFileName, actionsFileName, fps)
% This script addresses a bug that often appears with the demarcating of 
% trial start and end times in an eye video.  In Unity, a trial start and a 
% trial end are indicated by a certain number of missing frames, which Unity
% intentionally causes to occur between trials.  This way Matlab sees
% that too much time has passed since the last frame, and writes to the metadata
% file that a trial start or end has occurred, depending on whether the last
% event was a trial start or end.  The current number of intentionally dropped
% frames to indicate trial boundaries is GameControlScript.cs pauseTime = 3.

% However, sometimes Unity will drop this many frames or more by chance.  In this case,
% the Matlab video metadata indicating trial boundaries will be wrong.  So this script
% will compare the logged frame End and start times, which we don't normally use because 
% they are inaccurate on their own, with the Matlab trial markers.  Even thought
% the actionsFile has the logged frames off by a bit, these can be used to discard
% matlab trial boundaries that are no where close to those logged in the actionsFile.

% USAGE:
% > cleanUpTrialTimes('Dragon_182.mat', 'Dragon_182_actions.txt', 60)

successWait = 2;  % if trial success, gap between trial end and start will be 2 seconds
failureWait = 4;  % if trial failure, gap between trial end and start will be 4 seconds

allowableTrialStartDiff = 3; % This is the initial value - it will be updated as the gap is updated
allowableTrialEndDiff = 3;
allowableDrift = 12;  % the allowable trial difference can be off by this amount

newTrialStarts = struct([]);
newTrialEnds = struct([]);

load(videoMetadataFileName, 'trialStarts', 'trialEnds', 'framesAcquiredLogged');
% Generate a consolidated array of all trialStarts and trialEnds in chronological order, to simplify later processing
allTrialMarkers = [trialStarts(1:length(trialEnds)); trialEnds];
allTrialMarkers = allTrialMarkers(:)';  % Now alternates each
if (length(trialEnds) < length(trialStarts))  % If the original arrays were unequal, add on the extra elements at the end
    allTrialMarkers = [allTrialMarkers trialStarts(length(trialEnds)+1:end)];
end

% Initialize the trialStarts with the first marker, as it is assumed to be correct, and is frame 0
newTrialStarts = [newTrialStarts allTrialMarkers(1)];
fileTrialStartFrame = 0;

fid = fopen(actionsFileName);
if (fid ~= -1)
    fgetl(fid); % Throw out the first line, as it is a column header
    C = textscan(fid, '%s %s %d %s %s %d %d %d %d %d %d %s %d %d %f %d %d %d %d %d %d'); 

    mdIdx = 1;  % keep a metadata index separate from the lineIdx
    for lineIdx = 1:length(C{1})  % For each line
        mdIdx = mdIdx + 1;
        % Find the metadata date for use in the fileTrialEndDateTime object
        candTrialEndDateTime = datetime(allTrialMarkers(mdIdx).AbsTime);
        
        % We use the file timestamp and frame numbers as GROUND TRUTH 
        % (though not accurate, we use as a guidepost)
        fileTrialEndFrame = C{3}(lineIdx);
        fileTrialEndTime = C{2}(lineIdx);  
        % Convert string to datetime object (e.g. 13:42:31.6690032)
        fileTrialEndTime = split(fileTrialEndTime,':');
        fileTrialEndTime = [fileTrialEndTime(1); fileTrialEndTime(2); split(fileTrialEndTime(3), '.')];
        % Use the date from the metadata, as that is not explicitly stored in the file
        ms = fileTrialEndTime(4);  % milliseconds, but too many digits
        ms = ms{1}(1:3);  % Use first 3 digits as the millisecond count
        fileTrialEndDateTime = datetime(year(candTrialEndDateTime), month(candTrialEndDateTime), ...
                                        day(candTrialEndDateTime), str2double(fileTrialEndTime{1}), ...
                                        str2double(fileTrialEndTime{2}), str2double(fileTrialEndTime{3}), ...
                                        str2double(ms));

        smallestDelta = between(fileTrialEndDateTime, candTrialEndDateTime);
        smallestDelta = abs(milliseconds(time(smallestDelta)));  % get dt in terms of milliseconds
        
        % Now, look ahead to see if any timestamp is closer to the fileEndTime
        for j = mdIdx+1:length(allTrialMarkers)
            candTrialEndDateTime = datetime(allTrialMarkers(j).AbsTime);
            dt = between(fileTrialEndDateTime, candTrialEndDateTime);
            dt = abs(milliseconds(time(dt)));  % get dt in terms of milliseconds
            if (dt < smallestDelta)
                smallestDelta = dt;
                mdIdx = j;
            else
                break;
            end
        end
        
        newTrialEnds = [newTrialEnds allTrialMarkers(mdIdx)];

        disp(strcat(num2str(lineIdx), ':', num2str(fileTrialStartFrame), '-', ...
            num2str(newTrialStarts(lineIdx).FrameNumber), '=', ...
            num2str(fileTrialStartFrame - newTrialStarts(lineIdx).FrameNumber), ';', ...
            num2str(fileTrialEndFrame), '-', num2str(newTrialEnds(lineIdx).FrameNumber), '=', ...
            num2str(fileTrialEndFrame - newTrialEnds(lineIdx).FrameNumber)));

        if (lineIdx == 59)
            disp('');
        end
        
        mdIdx = mdIdx + 1; % Increment to look at next marker for trialStarts
        if (mdIdx <= length(allTrialMarkers))  % If we haven't ran out of metadata, look for trialStart
            % Get next trial start using knowledge of whether the trial was correct or incorrect
            if (iscell(C{5}(lineIdx)))
                tmp = strsplit(C{5}{lineIdx}, ';');
                stimLocX = str2double(tmp{1});
            else
                stimLocX = str2double(C{5}(lineIdx));
            end
            if (iscell(C{12}(lineIdx)))
                tmp = strsplit(C{12}{lineIdx}, ';');
                actionLocX = str2double(tmp{1});
            else 
                actionLocX = str2double(C{12}(lineIdx));
            end
            if (stimLocX == actionLocX) % Correct trial
                fileTrialStartDateTime = fileTrialEndDateTime + seconds(successWait);
                fileTrialStartFrame = fileTrialEndFrame + successWait * fps;
            else
                fileTrialStartDateTime = fileTrialEndDateTime + seconds(failureWait);
                fileTrialStartFrame = fileTrialEndFrame + failureWait * fps;
            end

            candTrialStartDateTime = datetime(allTrialMarkers(mdIdx).AbsTime);

            smallestDelta = between(fileTrialStartDateTime, candTrialStartDateTime);
            smallestDelta = abs(milliseconds(time(smallestDelta)));  % get dt in terms of milliseconds

            % Now, look ahead to see if any timestamp is closer to the fileEndTime
            for j = mdIdx+1:length(allTrialMarkers)
                candTrialStartDateTime = datetime(allTrialMarkers(j).AbsTime);
                dt = between(fileTrialStartDateTime, candTrialStartDateTime);
                dt = abs(milliseconds(time(dt)));  % get dt in terms of milliseconds
                if (dt < smallestDelta)
                    smallestDelta = dt;
                    mdIdx = j;  % This is how we skip over invalid entries in the metadata
                else
                    break;
                end
            end

            newTrialStarts = [newTrialStarts allTrialMarkers(mdIdx)];
        end
    end
else
    error('Actions file cannot be found');
end

% OK, if all corrected, the number of trials should be equal to the number of lines in the actions file
% Then, rename old .mat file and make new .mat file with the corrected trial boundaries
if (length(newTrialEnds) == length(C{1}))
    movefile(videoMetadataFileName, [videoMetadataFileName(1:end-4) '_orig.mat']);
    if (length(trialEnds) == length(newTrialEnds)) % No trimming was done - original file was good
        disp(['No trimming done, original file good with ' length(trialEnds) ' trial ends.']);
        disp(['Old .mat file moved to ' videoMetadataFileName(1:end-4) '_orig.mat']); 
        disp(['Identical .mat file named ' videoMetadataFileName(1:end-4) '_corr.mat']);
    else
        disp(['Original trial markers wrong, trimmed from ' num2str(length(trialEnds)) ' to ' num2str(length(newTrialEnds)) ' trial ends.']);
        disp(['Old .mat file moved to ' videoMetadataFileName(1:end-4) '_orig.mat']); 
        disp(['Corrected .mat file named ' videoMetadataFileName(1:end-4) '_corr.mat']);
    end
    trialStarts = newTrialStarts;
    trialEnds = newTrialEnds;
    save([videoMetadataFileName(1:end-4) '_corr.mat'], 'framesAcquiredLogged', 'trialStarts', 'trialEnds');
else
    error('Analyzed trialStarts and trialEnds, but still not consistent with the actions file :(');
end

end