function dur = getDuration(trialRecs, trialIdx)
% Helper function used by lots of code.  Returns the duration of a trial in seconds

dur = trialRecs{4}{trialIdx};
dur = split(dur, ':');
dur = seconds(duration(str2double(dur{1}), str2double(dur{2}), str2double(dur{3})));

end