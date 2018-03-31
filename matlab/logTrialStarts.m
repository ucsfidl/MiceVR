function logTrialStarts(vid, event)
% Callback function to be used when an frame capture is triggered.  This
% will allow us to keep track ofwhen a behavior trial ends because there
% will be a long temporal gap between trigger events.

global trialStarts;
global lastEventTime;

d = event.Data;
if (lastEventTime == 0)
    %disp(0);
    %disp(datestr(lastEventTime,'YYYY/mm/dd HH:MM:SS:FFF'))
elseif (etime(d.AbsTime, lastEventTime) > 1) % seconds
    %disp(etime(d.AbsTime, lastEventTime));
    trialStarts = [trialStarts d];
end

lastEventTime = d.AbsTime;

end