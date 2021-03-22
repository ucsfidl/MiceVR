function [actionLocX, actionLocZ] = getActionLoc(trialRecs, trialIdx)
% Helper function used by lots of code

if (iscell(trialRecs{12}(trialIdx)) && length(strsplit(trialRecs{12}{trialIdx}, ';')) > 1)
    tmp = strsplit(trialRecs{12}{trialIdx}, ';');
    actionLocX = str2double(tmp{1});
    actionLocZ = str2double(tmp{3});
else 
    actionLocX = str2double(trialRecs{12}(trialIdx));
    actionLocZ = -1;  % Old records file did not include stimLocZ
end


end