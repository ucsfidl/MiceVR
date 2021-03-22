function [stimLocX, stimLocZ] = getStimLoc(trialRecs, trialIdx)
% Helper function used by lots of code

stimLocZ = -1;  % Old records file did not include stimLocZ
if (iscell(trialRecs{5}(trialIdx)) && length(strsplit(trialRecs{5}{trialIdx}, ';')) > 1)
    tmp = strsplit(trialRecs{5}{trialIdx}, ';');
    stimLocX = str2double(tmp{1});
    stimLocZ = str2double(tmp{3});
else
    stimLocX = str2double(trialRecs{5}(trialIdx));
end


end