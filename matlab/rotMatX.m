function M = rotMatX(degrees)

M = [1 0 0;
     0 cosd(degrees) -sind(degrees);
     0 sind(degrees) cosd(degrees)];

end