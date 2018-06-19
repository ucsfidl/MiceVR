function col = pickColorByAccuracy(acc, type)
% This is a helper function that picks a color based on the accuracy.
% The ranges are either from white (100%) to black (0%), or green (100%) to
% red (0%).

% type = [wb, gr]

col = [0.5 0.5 0.5];
if (strcmp('wb', type))
    col = [acc/2 acc/2 acc/2];
elseif (strcmp('gr', type))
    r = 0:1/255:1;
    g = 1:-1/255:0;
    b = zeros(1,256);
    gr = cat(2, r', g', b');

    gd = abs(g - acc);
    idx = find(gd == min(gd));
    col = gr(idx(1),:);
end

end