function drawStimulusPatch(mapKey, hits, misses)
    c = strsplit(mapKey, ',');
    n = str2double(c{1});
    t = str2double(c{2});
    h = str2double(c{3});
    l = str2double(c{4});
    x = [n n t t];
    y = [h l l h];
    acc = hits / (hits + misses);
    color = pickColorByAccuracy(acc, 'gr');
    fill(x, y, color, 'FaceAlpha', 0.5);
    % Draw hits and totals on screen in fill area
    tx = (n + t) / 2;
    low = l;
    if (low < 0)
        low = 0;
    end
    ty = low + 2;  % Special case for full stim
    str = [num2str(acc*100, '%.0f') '% (' num2str(hits) '/' num2str(hits + misses) ')'];
    text(tx, ty, str, 'HorizontalAlignment', 'center');
end