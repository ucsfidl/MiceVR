function idx = mapLocXToIdx(locX)

leftX = 19975;
straightX = 20000;
rightX = 20025;

leftNearX = 19973;
leftFarX = 19972;
rightNearX = 20027;
rightFarX = 20028;

catchX = -1;


if locX == leftX
    idx = 0;
elseif locX == rightX
    idx = 1;
elseif locX == straightX
    idx = 2;
elseif locX == leftNearX
    idx = 0;
elseif locX == rightNearX
    idx = 1;
elseif locX == leftFarX
    idx = 2;
elseif locX == rightFarX
    idx = 3;
elseif locX == catchX
    idx = -1;
end


end