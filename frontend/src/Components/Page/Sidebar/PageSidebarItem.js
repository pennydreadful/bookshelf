import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import { map } from 'Helpers/elementChildren';
import styles from './PageSidebarItem.css';

class PageSidebarItem extends Component {

  //
  // Listeners

  onPress = () => {
    const {
      isChildItem,
      isParentItem,
      onPress
    } = this.props;

    if (isChildItem || !isParentItem) {
      onPress();
    }
  };

  //
  // Render

  render() {
    const {
      iconName,
      title,
      titleKey,
      to,
      isActive,
      isActiveParent,
      isChildItem,
      statusComponent: StatusComponent,
      children
    } = this.props;

    const titleValue = typeof title === 'function' ? title() : title;
    const titleContent = titleKey === 'CutoffUnmet' && typeof titleValue === 'string' ?
      this.renderCenteredLastWord(titleValue) :
      titleValue;

    return (
      <div
        className={classNames(
          styles.item,
          isActiveParent && styles.isActiveItem
        )}
      >
        <Link
          className={classNames(
            isChildItem ? styles.childLink : styles.link,
            isActiveParent && styles.isActiveParentLink,
            isActive && styles.isActiveLink
          )}
          to={to}
          onPress={this.onPress}
        >
          {
            !!iconName &&
              <span className={styles.iconContainer}>
                <Icon
                  name={iconName}
                />
              </span>
          }

          <span className={isChildItem ? styles.noIcon : null}>
            {titleContent}
          </span>

          {
            !!StatusComponent &&
              <span className={styles.status}>
                <StatusComponent />
              </span>
          }
        </Link>

        {
          children &&
            map(children, (child) => {
              return React.cloneElement(child, { isChildItem: true });
            })
        }
      </div>
    );
  }

  renderCenteredLastWord(value) {
    const trimmed = value.trim();
    const lastSpaceIndex = trimmed.lastIndexOf(' ');

    if (lastSpaceIndex === -1) {
      return value;
    }

    const firstLine = trimmed.slice(0, lastSpaceIndex);
    const lastWord = trimmed.slice(lastSpaceIndex + 1);

    return (
      <span className={styles.cutoffTitle}>
        <span className={styles.cutoffTitleFirst}>{firstLine}</span>
        <span className={styles.cutoffTitleSecond}>{lastWord}</span>
      </span>
    );
  }
}

PageSidebarItem.propTypes = {
  iconName: PropTypes.object,
  title: PropTypes.oneOfType([PropTypes.string, PropTypes.func]).isRequired,
  titleKey: PropTypes.string,
  to: PropTypes.string.isRequired,
  isActive: PropTypes.bool,
  isActiveParent: PropTypes.bool,
  isParentItem: PropTypes.bool.isRequired,
  isChildItem: PropTypes.bool.isRequired,
  statusComponent: PropTypes.elementType,
  children: PropTypes.node,
  onPress: PropTypes.func
};

PageSidebarItem.defaultProps = {
  isChildItem: false
};

export default PageSidebarItem;
