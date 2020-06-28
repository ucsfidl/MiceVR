function str = getActionLineFormat()
% Helper to consolidate formatting changes in the actions file in one place

str = '%s\t%s\t%d\t%s\t%s\t%d\t%d\t%d\t%d\t%d\t%d\t%s\t%d\t%d\t%f\t%d\t%d\t%d\t%d\t%d\t%d\t%d\t%f\t%f';

% Entries 23 and 24 must be %f instead of %d, since they are missing from action files prior to 2020/6/29
% Floating point numbers can be NaN, but intergers cannot

end