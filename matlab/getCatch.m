function ca = getCatch(trialRecs, trialIdx)

stimLocX = getStimLoc(trialRecs, trialIdx);

if stimLocX == -1
    ca = 1;
else
    ca = 0;
end

end