function targetBound = denoiseBounds(targetBound, maxAllowedJump)
% Helper function called by plotTargetAzimLoc
% Since it is used 2x, I pulled it out to a helper function.
% Unity can produce some noisy values for where the edges of the target are,
% so this function tries to denoise that signal, replacing noise with prior
% good values (not interpolating, at least at the moment).

dB = abs(diff(abs(targetBound)));
noiseIdxs = find(dB > maxAllowedJump);
idx = 1;
while 1
    if (idx <= length(noiseIdxs))  % there are noiseIdxs left
        priorVal = targetBound(noiseIdxs(idx));
        startIdx = noiseIdxs(idx)+1;
        if (idx < length(noiseIdxs)) % at least one more noisy value boundary observed after this one
            idx = idx + 1;
            endIdx = noiseIdxs(idx);
            while 1
                if (abs(targetBound(endIdx+1) - priorVal) <= maxAllowedJump)
                    break;
                else
                    if (idx + 1 <= length(noiseIdxs))
                        idx = idx + 1;
                        endIdx = noiseIdxs(idx);
                    else
                        targetBound(startIdx:end) = priorVal;
                        break;
                   end
                end
            end
            targetBound(startIdx:endIdx) = priorVal;
            idx = idx + 1;
        else
            targetBound(startIdx:end) = priorVal;
            break;
        end
    else
        break;
    end
end


end