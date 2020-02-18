function [stimLocX, stimLocZ] = getStimLocFromActions(actRecs, k)
% Helper function used by lots of code

stimLocZ = -1;  % Old records file did not include stimLocZ
if (iscell(actRecs{5}(k)))
    tmp = strsplit(actRecs{5}{k}, ';');
    stimLocX = str2double(tmp{1});
    stimLocZ = str2double(tmp{3});
else
    stimLocX = str2double(actRecs{5}(k));
end


end