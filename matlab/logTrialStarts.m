function logTrialStarts(vid, event)
% Callback function to be used when an frame capture is triggered.  This
% will allow us to keep track ofwhen a behavior trial ends because there
% will be a long temporal gap between trigger events.

% Note that all of these values appear to be off by 1, i.e. in the video
% the next trial starts on frame 1417, but this object
% records the frame as frame 1416.  So need to add 1 to each of these
% values to get the true trialStarts.  As it is now, these are actuall
% trialEnd frame counts!

global trialStarts;
global lastEventTime;

d = event.Data;
if (lastEventTime == 0) % This is the first frame, so log it as the trial start
    trialStarts = [trialStarts d];
elseif (etime(d.AbsTime, lastEventTime) > 1) % seconds
    %disp(etime(d.AbsTime, lastEventTime));
    trialStarts = [trialStarts d];
end

lastEventTime = d.AbsTime;

end