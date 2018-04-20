function logTrialMarks(vid, event)
% Callback function to be used when an frame capture is triggered.  This
% will allow us to keep track of when a behavior trial ends and starts because there
% will be a long temporal gap between trigger events.

% Note that all of these values appear to be off by 1, i.e. in the video
% the next trial starts on frame 1417, but this object
% records the frame as frame 1416.  So need to add 1 to each of these
% values to get the true trialStarts.  As it is now, these are actually
% trialEnd frame counts!

    %disp(datestr(lastEventTime,'YYYY/mm/dd HH:MM:SS:FFF'))

global trialStarts;
global trialEnds;
global lastEventTime;

global fps;

d = event.Data;
if (lastEventTime == 0) % This is the first frame, so log it as the trial start
    trialStarts = [trialStarts d];
    % Note that this is labeled as frame #0, not frame #1
% For unclear reasons there is a slowdown in Unity going in and out of
% pause, so rely on this to track trial boundaries in the video.
elseif (etime(d.AbsTime, lastEventTime) > 1/fps * 2.9) % 2.5 has false positives at a pauseTime of 2 - try 2.9
    if (numel(trialStarts) == numel(trialEnds))
        trialStarts = [trialStarts d];  % This is actually the second frame after the pause start
    else
        trialEnds = [trialEnds d]; % This is actually the second(?) frame of the new trial
    end
    %disp(etime(d.AbsTime, lastEventTime));
end

lastEventTime = d.AbsTime;

end