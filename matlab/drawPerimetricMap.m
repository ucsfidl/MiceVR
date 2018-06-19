function drawPerimetricMap(hitMap, missMap, fov, reverseX)

hitK = keys(hitMap);
missK = keys(missMap);

h = figure;
hold on;
for i = 1:length(hitK)
    hits = hitMap(hitK{i});
    misses = 0;
    if (isKey(missMap, hitK{i}))
        misses = missMap(hitK{i});
    end
    drawStimulusPatch(hitK{i}, hits, misses);
end
for i = 1:length(missK)
    if (~isKey(hitMap, missK{i}))  % Don't draw duplicates again, only those not drawn before because not hits
        misses = missMap(missK{i});
        drawStimulusPatch(missK{i}, 0, misses); 
    end
end
ylim([0 50]);
xlim([0 90]);
side = 'Right';
if (reverseX)
    set(gca, 'XDir','reverse'); % For left hemisphere
    side = 'Left';
end

title(['Perimetry: ' side ' FOV, window size = ' num2str(fov) ' deg square']);

end