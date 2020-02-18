function [actionLocX, actionLocZ] = getActionLocFromActions(actRecs, k)
% Helper function used by lots of code

actionLocZ = -1;  % Old records file did not include stimLocZ
if (iscell(actRecs{12}(k)))
    tmp = strsplit(actRecs{12}{k}, ';');
    actionLocX = str2double(tmp{1});
    actionLocZ = str2double(tmp{3});
else 
    actionLocX = str2double(actRecs{12}(k));
end


end